using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

static class CustomizingDownstreamUrlsSnippets
{
    static void ShowCustomUrls()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-customizing-downstream-urls
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
                {
                    DestinationUrlMapper = (request, destination) => destination.Replace(
                        "{this-is-contextual}",
                        request.Query["this-is-contextual"])
                }
            }
        });
        // end-snippet

        app.Run();
    }
}
