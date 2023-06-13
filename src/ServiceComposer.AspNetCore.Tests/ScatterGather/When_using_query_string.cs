using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore.Tests.Utils;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class When_using_query_string
{
    [Fact]
    public async Task Values_are_propagated_to_downstream_destinations()
    {
        // Arrange
        var aSampleSourceClient = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/samples/ASamplesSource", (string culture) =>
                    {
                        return new []{ new { Value = "ASample", Culture = culture } };
                    });
                });
            }
        ).CreateClient();
        
        var anotherSampleSourceClient = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/samples/AnotherSamplesSource", (string culture) =>
                    {
                        return new []{ new { Value = "AnotherSample", Culture = culture } };
                    });
                });
            }
        ).CreateClient();

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                HttpClient ClientProvider(string name) =>
                    name switch
                    {
                        "ASamplesSource" => aSampleSourceClient,
                        "AnotherSamplesSource" => anotherSampleSourceClient,
                        _ => throw new NotSupportedException($"Missing HTTP client for {name}")
                    };
                
                // TODO: does this need to register a default HTTP client?
                // services.AddScatterGather();
                services.AddRouting();
                services.Replace(
                    new ServiceDescriptor(typeof(IHttpClientFactory), 
                    new DelegateHttpClientFactory(ClientProvider)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(template: "/samples", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer(key: "ASamplesSource", destinationUrl: "/samples/ASamplesSource"),
                            new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "/samples/AnotherSamplesSource")
                        }
                    });
                });
            }
        ).CreateClient();
        
        // Act
        var culture = "it-IT";
        var response = await client.GetAsync($"/samples?culture={culture}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseString = await response.Content.ReadAsStringAsync();
        var responseArray = JsonNode.Parse(responseString)!.AsArray();
        var responseArrayAsJsonStrings = new HashSet<string>(responseArray.Select(n=>n.ToJsonString()));

        var expectedArray = JsonNode.Parse(JsonSerializer.Serialize( new[]
        {
            new {Value = "ASample", Culture = culture},
            new {Value = "AnotherSample", Culture = culture}
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }))!.AsArray();
        var expectedArrayAsJsonStrings = new HashSet<string>(expectedArray.Select(n=>n.ToJsonString()));

        Assert.Equal(2, responseArray.Count);
        Assert.Equivalent(expectedArrayAsJsonStrings, responseArrayAsJsonStrings);
    }
}

