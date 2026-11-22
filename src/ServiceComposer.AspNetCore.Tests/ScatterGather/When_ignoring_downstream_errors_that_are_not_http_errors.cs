using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore.Testing;
using ServiceComposer.AspNetCore.Tests.Utils;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

// IgnoreDownstreamRequestErrors is documented to cover HTTP error status codes, network
// timeouts, and any other exception thrown by the downstream call
public class When_ignoring_downstream_errors_that_are_not_http_errors
{
    public enum Failure
    {
        Timeout,
        NotJson,
        NotAnArray
    }

    static HttpClient BuildFailingClient(Failure failure)
    {
        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/upstream/failing", async (HttpContext context) =>
                    {
                        switch (failure)
                        {
                            case Failure.Timeout:
                                await Task.Delay(TimeSpan.FromSeconds(5), context.RequestAborted);
                                break;
                            case Failure.NotJson:
                                await context.Response.WriteAsync("this is not JSON");
                                break;
                            case Failure.NotAnArray:
                                await context.Response.WriteAsJsonAsync(new { Value = "not an array" });
                                break;
                        }
                    });
                });
            }
        ).CreateClient();

        if (failure == Failure.Timeout)
        {
            client.Timeout = TimeSpan.FromMilliseconds(200);
        }

        return client;
    }

    static HttpClient BuildHealthyClient()
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder => builder.MapGet("/upstream/healthy", () => new[] { new { Value = "OK" } }));
            }
        ).CreateClient();
    }

    static HttpClient CreateClient(Failure failure, bool ignoreDownstreamRequestErrors)
    {
        var failingClient = BuildFailingClient(failure);
        var healthyClient = BuildHealthyClient();

        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(name => name switch
                    {
                        "Failing" => failingClient,
                        "Healthy" => healthyClient,
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
                            new HttpGatherer("Failing", "/upstream/failing") { IgnoreDownstreamRequestErrors = ignoreDownstreamRequestErrors },
                            new HttpGatherer("Healthy", "/upstream/healthy")
                        }
                    });
                });
            }
        ).CreateClient();
    }

    [Theory]
    [InlineData(Failure.Timeout)]
    [InlineData(Failure.NotJson)]
    [InlineData(Failure.NotAnArray)]
    public async Task The_error_is_ignored(Failure failure)
    {
        var response = await CreateClient(failure, ignoreDownstreamRequestErrors: true).GetAsync("/items");

        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var array = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        Assert.Single(array);
        Assert.Equal("OK", array[0]!["value"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(Failure.Timeout)]
    [InlineData(Failure.NotJson)]
    [InlineData(Failure.NotAnArray)]
    public async Task The_error_propagates_when_not_ignored(Failure failure)
    {
        var client = CreateClient(failure, ignoreDownstreamRequestErrors: false);

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync("/items"));
    }
}
