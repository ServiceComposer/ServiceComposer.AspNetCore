using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

static class ScatterGatherOutputFormattersSnippets
{
    static void ShowOutputFormatters()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-use-output-formatters
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            UseOutputFormatters = true,
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource"),
                new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
            }
        });
        // end-snippet

        app.Run();
    }
}
