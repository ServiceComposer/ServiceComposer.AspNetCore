using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public static class ScatterGatherEndpointBuilderExtensions
{
    public static IEndpointConventionBuilder MapScatterGather(this IEndpointRouteBuilder builder, string template, ScatterGatherOptions options)
    {
        return builder.MapGet(template, async context =>
        {
            var aggregator = options.GetAggregator(context);
            var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            var tasks = new List<Task>();
            foreach (var gatherer in options.Gatherers)
            {
                var client = factory.CreateClient(gatherer.Key);
                
                var destination = gatherer.DestinationUrlMapper(context.Request);
                var task = client.GetAsync(destination)
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

            await context.Response.WriteAsync(responses);
        });
    }
}