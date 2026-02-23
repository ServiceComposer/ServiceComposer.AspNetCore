using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class DisableHeaderForwarding
{
    // begin-snippet: scatter-gather-disable-header-forwarding
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
                {
                    ForwardHeaders = false
                }
            }
        }));
    }
    // end-snippet
}

public class FilterHeaders
{
    // begin-snippet: scatter-gather-filter-headers
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
                {
                    HeadersMapper = (incomingRequest, outgoingMessage) =>
                    {
                        foreach (var header in incomingRequest.Headers)
                        {
                            if (header.Key.Equals("x-do-not-forward", StringComparison.OrdinalIgnoreCase))
                                continue;
                            outgoingMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                        }
                    }
                }
            }
        }));
    }
    // end-snippet
}

public class AddHeaders
{
    // begin-snippet: scatter-gather-add-headers
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
                {
                    HeadersMapper = (incomingRequest, outgoingMessage) =>
                    {
                        HttpGatherer.DefaultHeadersMapper(incomingRequest, outgoingMessage);
                        outgoingMessage.Headers.TryAddWithoutValidation("x-custom-header", "custom-value");
                    }
                }
            }
        }));
    }
    // end-snippet
}
