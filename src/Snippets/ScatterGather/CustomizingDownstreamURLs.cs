using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class CustomizingDownstreamURLs
{
    // begin-snippet: scatter-gather-customizing-downstream-urls
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
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
        }));
    }
    // end-snippet
}
