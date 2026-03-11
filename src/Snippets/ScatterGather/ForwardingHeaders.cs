using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

static class ForwardingHeadersSnippets
{
    static void ShowDisableHeaderForwarding()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-disable-header-forwarding
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
                {
                    ForwardHeaders = false
                }
            }
        });
        // end-snippet

        app.Run();
    }

    static void ShowFilterHeaders()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-filter-headers
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
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
        });
        // end-snippet

        app.Run();
    }

    static void ShowAddHeaders()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-add-headers
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
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
        });
        // end-snippet

        app.Run();
    }
}
