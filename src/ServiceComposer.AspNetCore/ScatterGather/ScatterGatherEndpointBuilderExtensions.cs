using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

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

    /// <summary>
    /// Reads scatter/gather route definitions from <paramref name="configuration"/> and registers
    /// a GET endpoint for each one. The <paramref name="configuration"/> value is expected to be
    /// (or bind as) a list of <see cref="ScatterGatherRouteConfiguration"/> entries — pass
    /// <c>configuration.GetSection("ScatterGather")</c> when the definitions are nested under a
    /// named key.
    /// </summary>
    /// <param name="builder">The endpoint route builder.</param>
    /// <param name="configuration">
    /// The configuration section that contains the list of route definitions.
    /// </param>
    /// <param name="customize">
    /// An optional callback invoked for every route after the <see cref="ScatterGatherOptions"/>
    /// has been built from configuration but before the endpoint is registered. Use this to add
    /// extra gatherers, override <see cref="ScatterGatherOptions.UseOutputFormatters"/>, or apply
    /// any other per-route customisation.
    /// </param>
    /// <returns>
    /// A read-only list of <see cref="IEndpointConventionBuilder"/> instances — one per route
    /// defined in <paramref name="configuration"/>.
    /// </returns>
    public static IReadOnlyList<IEndpointConventionBuilder> MapScatterGatherFromConfiguration(
        this IEndpointRouteBuilder builder,
        IConfiguration configuration,
        Action<string, ScatterGatherOptions> customize = null)
    {
        var routes = configuration.Get<IList<ScatterGatherRouteConfiguration>>();
        if (routes is null or { Count: 0 })
        {
            return [];
        }

        var result = new List<IEndpointConventionBuilder>(routes.Count);
        foreach (var route in routes)
        {
            var options = new ScatterGatherOptions
            {
                UseOutputFormatters = route.UseOutputFormatters,
                Gatherers = route.Gatherers
                    .Select(g => (IGatherer)new HttpGatherer(g.Key, g.DestinationUrl))
                    .ToList()
            };
            customize?.Invoke(route.Template, options);
            result.Add(builder.MapScatterGather(route.Template, options));
        }

        return result;
    }
}
