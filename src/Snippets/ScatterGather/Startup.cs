using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

static class ScatterGatherBasicSnippets
{
    static void ShowBasicUsage()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-basic-usage
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource"),
                new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
            }
        });
        // end-snippet

        app.Run();
    }

    static void ShowIgnoreErrors()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-ignore-errors
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource")
                {
                    IgnoreDownstreamRequestErrors = true
                },
                new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
            }
        });
        // end-snippet

        app.Run();
    }
}
