using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class UseOutputFormattersConfig
{
    // begin-snippet: scatter-gather-use-output-formatters
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            UseOutputFormatters = true,
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource"),
                new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
            }
        }));
    }
    // end-snippet
}
