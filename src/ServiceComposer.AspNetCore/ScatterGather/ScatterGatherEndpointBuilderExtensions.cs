using System.Collections.Generic;
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
            
            var tasks = new List<Task>();
            foreach (var gatherer in options.Gatherers)
            {
                var task = gatherer.Gather(context)
                    .ContinueWith(t =>
                    {
                        // TODO: how to handle errors?
                        // t.IsFaulted?
                     
                        aggregator.Add(t.Result);
                    });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            var responses = await aggregator.Aggregate();

            // TODO: support output formatters by using the WriteModelAsync extension method.
            // It must be under a setting flag, because it requires a dependency on MVC.
            await context.Response.WriteAsync(responses.ToJsonString());
        });
    }
}