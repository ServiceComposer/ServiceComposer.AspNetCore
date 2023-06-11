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
    public static void MapScatterGather(this IEndpointRouteBuilder builder, string template, ScatterGatherOptions options)
    {
        builder.MapGet(template, async context =>
        {
            var aggregator = options.GetAggregator(context);
            var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            var tasks = new List<Task>();
            foreach (var gatherer in options.Gatherers)
            {
                var client = factory.CreateClient(gatherer.Key);
                var task = client.GetAsync(gatherer.Destination)
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