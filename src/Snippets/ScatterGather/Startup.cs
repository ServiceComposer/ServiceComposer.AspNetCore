using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class Startup
{
    // begin-snippet: scatter-gather-basic-usage
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseRouting();
        app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            Gatherers = new List<Gatherer>
            {
                new(key: "ASamplesSource", destination: "https://a.web.server/api/samples/ASamplesSource"),
                new(key: "AnotherSamplesSource", destination: "https://another.web.server/api/samples/AnotherSamplesSource")
            }
        }));
    }
    // end-snippet
}