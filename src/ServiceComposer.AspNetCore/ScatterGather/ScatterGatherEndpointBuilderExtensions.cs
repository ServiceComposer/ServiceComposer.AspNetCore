using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

            if (options.UseOutputFormatters)
            {
                // Use MVC output formatters for content negotiation (JSON, XML, etc.)
                // ObjectResult without an explicit DeclaredType uses the value's runtime type,
                // which allows formatters to correctly serialize the actual object.
                await context.ExecuteResultAsync(new ObjectResult(responses));
            }
            else
            {
                await context.Response.WriteAsync(JsonSerializer.Serialize(responses));
            }
        });
    }
}
