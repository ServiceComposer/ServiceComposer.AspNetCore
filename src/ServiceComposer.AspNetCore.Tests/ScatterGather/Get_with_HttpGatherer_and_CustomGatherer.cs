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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore.Tests.Utils;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class Get_with_HttpGatherer_and_CustomGatherer
{
    class CustomGatherer : IGatherer
    {
        public string Key { get; } = "CustomGatherer";
        public Task<IEnumerable<object>> Gather(HttpContext context)
        {
            var data = (IEnumerable<object>)(new []{ new { Value = "ACustomSample" } });
            return Task.FromResult(data);
        }
    }

    [Fact]
    public async Task Returns_expected_response()
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
                    builder.MapGet("/samples/ASamplesSource", () =>
                    {
                        return new []{ new { Value = "ASample" } };
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
                        _ => throw new NotSupportedException($"Missing HTTP client for {name}")
                    };
                
                // TODO: does this need to register a default HTTP client?
                // services.AddScatterGather();
                services.AddRouting();
                services.AddControllers();
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
                            new CustomGatherer()
                        }
                    });
                });
            }
        ).CreateClient();
        
        // Act
        var response = await client.GetAsync("/samples");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var responseString = await response.Content.ReadAsStringAsync();
        var responseArray = JsonNode.Parse(responseString)!.AsArray();
        var responseArrayAsJsonStrings = new HashSet<string>(responseArray.Select(n=>n.ToJsonString()));

        var expectedArray = JsonNode.Parse(JsonSerializer.Serialize( new[]
        {
            new {Value = "ASample"},
            new {Value = "ACustomSample"}
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }))!.AsArray();
        var expectedArrayAsJsonStrings = new HashSet<string>(expectedArray.Select(n=>n.ToJsonString()));

        Assert.Equal(2, responseArray.Count);
        Assert.Equivalent(expectedArrayAsJsonStrings, responseArrayAsJsonStrings);
    }
}

