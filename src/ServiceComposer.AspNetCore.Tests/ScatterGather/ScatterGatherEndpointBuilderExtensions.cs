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
                
                // TODO: template matching?
                // e.g., what if Destination is defined as
                // /samples/{culture}/ASamplesSource
                // and the source template is /samples/{culture}
                // or this responsibility could be moved into the gatherer
                // and users could pass in a Func to transform the incoming request path
                // into the outgoing request path
                // e.g. /samples/ASamplesSource -> /samples/ASamplesSource?filter=foo
                // or they could define a IDownstreamInvoker that does the entire downstream request
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