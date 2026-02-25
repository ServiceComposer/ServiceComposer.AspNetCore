using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore.Testing;
using ServiceComposer.AspNetCore.Tests.Utils;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class When_downstream_returns_error
{
    static HttpClient BuildDownstreamClientReturning(HttpStatusCode statusCode)
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/upstream/source", (Microsoft.AspNetCore.Http.HttpContext ctx) =>
                    {
                        ctx.Response.StatusCode = (int)statusCode;
                    });
                });
            }
        ).CreateClient();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Non_success_status_from_downstream_propagates_as_exception(HttpStatusCode statusCode)
    {
        // Arrange
        var downstreamClient = BuildDownstreamClientReturning(statusCode);

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(_ => downstreamClient)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather("/items", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer("Source", "/upstream/source")
                        }
                    });
                });
            }
        ).CreateClient();

        // Act & Assert — EnsureSuccessStatusCode() in HttpGatherer.Gather() throws
        // HttpRequestException for any non-2xx response; it propagates out of the endpoint.
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/items"));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task IgnoreDownstreamRequestErrors_returns_empty_results_for_failed_gatherer(HttpStatusCode statusCode)
    {
        // Arrange — failing gatherer alongside a healthy one
        var failingClient = BuildDownstreamClientReturning(statusCode);
        var successClient = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/upstream/healthy", () => new[] { new { Value = "OK" } });
                });
            }
        ).CreateClient();

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(name => name switch
                    {
                        "Failing" => failingClient,
                        "Healthy" => successClient,
                        _ => throw new NotSupportedException(name)
                    })));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather("/items", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer("Failing", "/upstream/source") { IgnoreDownstreamRequestErrors = true },
                            new HttpGatherer("Healthy", "/upstream/healthy")
                        }
                    });
                });
            }
        ).CreateClient();

        // Act
        var response = await client.GetAsync("/items");

        // Assert — composed request succeeds with the healthy gatherer's data only
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var array = System.Text.Json.Nodes.JsonNode.Parse(body)!.AsArray();
        Assert.Single(array);
        Assert.Equal("OK", array[0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task IgnoreDownstreamRequestErrors_returns_empty_array_when_all_gatherers_fail()
    {
        // Arrange
        var failingClient = BuildDownstreamClientReturning(HttpStatusCode.InternalServerError);
        var loggerFactory = TestLoggerFactory.Create(options => options.FilterByTypeName<HttpGatherer>());

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(_ => failingClient)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather("/items", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer("Source", "/upstream/source") { IgnoreDownstreamRequestErrors = true }
                        }
                    });
                });
            }
        ).CreateClient();

        // Act
        var response = await client.GetAsync("/items");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var array = System.Text.Json.Nodes.JsonNode.Parse(body)!.AsArray();
        Assert.Empty(array);

        var log = Assert.Single(loggerFactory.Sink.LogEntries);
        Assert.Equal("Ignoring downstream request error for gatherer {GathererKey} at {DestinationUrl}.", log.OriginalFormat);
        Assert.Equal(LogLevel.Warning, log.LogLevel);
        var gathererKey = Assert.Single(log.Properties, p => p.Key == "GathererKey").Value;
        var destinationUrl = Assert.Single(log.Properties, p => p.Key == "DestinationUrl").Value;
        Assert.Equal("Source", gathererKey);
        Assert.Equal("/upstream/source", destinationUrl);
    }
}
