using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ServiceComposer.AspNetCore;

public static class ScatterGatherEndpointBuilderExtensions
{
    public static IEndpointConventionBuilder MapScatterGather(this IEndpointRouteBuilder builder, string template, ScatterGatherOptions options)
    {
        return builder.MapGet(template, async context =>
        {
            var aggregator = options.GetAggregator(context);

            async Task GatherAndAdd(IGatherer gatherer)
            {
                var result = await gatherer.Gather(context);
                aggregator.Add(result);
            }

            await Task.WhenAll(options.Gatherers.Select(GatherAndAdd));
            var responses = await aggregator.Aggregate();

            // TODO: support output formatters by using the WriteModelAsync extension method.
            // It must be under a setting flag, because it requires a dependency on MVC.
            await context.Response.WriteAsync(JsonSerializer.Serialize(responses));
        });
    }
}
