using System;
using System.Collections.Generic;
using System.Linq;
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

public class When_forwarding_headers
{
    const string CustomHeaderName = "x-custom-header";
    const string CustomHeaderValue = "custom-value";

    // Downstream that echoes back a specific request header value in the response body
    static HttpClient BuildEchoHeaderClient(string headerName)
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/samples/source", (HttpContext ctx) =>
                    {
                        var headerValue = ctx.Request.Headers.TryGetValue(headerName, out var v) ? v.ToString() : "not-present";
                        return new[] { new { EchoedHeader = headerValue } };
                    });
                });
            }
        ).CreateClient();
    }

    static HttpClient BuildComposerClient(
        HttpClient downstreamClient,
        Func<ScatterGatherOptions, ScatterGatherOptions> configureOptions)
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddControllers();
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(_ => downstreamClient)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    var options = configureOptions(new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer(key: "source", destinationUrl: "/samples/source")
                        }
                    });
                    builder.MapScatterGather(template: "/samples", options);
                });
            }
        ).CreateClient();
    }

    [Fact]
    public async Task Forwards_all_headers_by_default()
    {
        var downstream = BuildEchoHeaderClient(CustomHeaderName);
        var client = BuildComposerClient(downstream, opts => opts);
        client.DefaultRequestHeaders.Add(CustomHeaderName, CustomHeaderValue);

        var response = await client.GetAsync("/samples");

        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(json)!.AsArray();
        
        Assert.Equal(CustomHeaderValue, array[0]!["echoedHeader"]!.GetValue<string>());
    }

    [Fact]
    public async Task Does_not_forward_headers_when_ForwardHeaders_is_false()
    {
        var downstream = BuildEchoHeaderClient(CustomHeaderName);
        var client = BuildComposerClient(downstream, opts =>
        {
            opts.Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "source", destinationUrl: "/samples/source")
                {
                    ForwardHeaders = false
                }
            };
            return opts;
        });
        client.DefaultRequestHeaders.Add(CustomHeaderName, CustomHeaderValue);

        var response = await client.GetAsync("/samples");

        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(json)!.AsArray();
        
        // Header was not forwarded, so the downstream sees "not-present"
        Assert.Equal("not-present", array[0]!["echoedHeader"]!.GetValue<string>());
    }

    [Fact]
    public async Task Custom_HeadersMapper_can_filter_headers()
    {
        const string filteredHeader = "x-do-not-forward";
        const string allowedHeader = CustomHeaderName;

        // Downstream echoes the filtered header
        var downstream = BuildEchoHeaderClient(filteredHeader);

        var client = BuildComposerClient(downstream, opts =>
        {
            opts.Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "source", destinationUrl: "/samples/source")
                {
                    // Custom mapper: skip filteredHeader, forward everything else
                    HeadersMapper = (incomingRequest, outgoingMessage) =>
                    {
                        foreach (var header in incomingRequest.Headers)
                        {
                            if (header.Key.Equals(filteredHeader, StringComparison.OrdinalIgnoreCase))
                                continue;
                            outgoingMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                        }
                    }
                }
            };
            return opts;
        });

        client.DefaultRequestHeaders.Add(filteredHeader, "should-be-filtered");
        client.DefaultRequestHeaders.Add(allowedHeader, CustomHeaderValue);

        var response = await client.GetAsync("/samples");

        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(json)!.AsArray();
        
        // filteredHeader was blocked by the custom mapper
        Assert.Equal("not-present", array[0]!["echoedHeader"]!.GetValue<string>());
    }

    [Fact]
    public async Task Custom_HeadersMapper_can_add_headers()
    {
        const string injectedHeader = "x-injected-header";
        const string injectedValue = "injected-value";

        var downstream = BuildEchoHeaderClient(injectedHeader);

        var client = BuildComposerClient(downstream, opts =>
        {
            opts.Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "source", destinationUrl: "/samples/source")
                {
                    // Custom mapper: call default logic and also inject an extra header
                    HeadersMapper = (incomingRequest, outgoingMessage) =>
                    {
                        // forward all incoming headers first
                        foreach (var header in incomingRequest.Headers)
                        {
                            outgoingMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                        }
                        // then inject an additional header
                        outgoingMessage.Headers.TryAddWithoutValidation(injectedHeader, injectedValue);
                    }
                }
            };
            return opts;
        });

        var response = await client.GetAsync("/samples");

        Assert.True(response.IsSuccessStatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var array = JsonNode.Parse(json)!.AsArray();
        
        Assert.Equal(injectedValue, array[0]!["echoedHeader"]!.GetValue<string>());
    }
}
