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
using Microsoft.Extensions.DependencyInjection;

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
    /// a GET endpoint for each one. Pass
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
    public static IReadOnlyList<IEndpointConventionBuilder> MapScatterGather(this IEndpointRouteBuilder builder, IConfiguration configuration, Action<string, ScatterGatherOptions> customize = null)
    {
        var routeSections = configuration.GetChildren().ToList();
        if (routeSections.Count == 0)
        {
            return [];
        }

        var scatterGatherConfiguration = builder.ServiceProvider.GetService<ScatterGatherConfiguration>()
            ?? throw new InvalidOperationException(
                "ScatterGather services are not registered. " +
                "Call services.AddScatterGather() in ConfigureServices before calling MapScatterGather with an IConfiguration argument.");
        var sp = builder.ServiceProvider;

        var result = new List<IEndpointConventionBuilder>(routeSections.Count);
        foreach (var routeSection in routeSections)
        {
            var template = routeSection["Template"];
            var useOutputFormatters = routeSection.GetValue<bool>("UseOutputFormatters");

            var gatherers = routeSection
                .GetSection("Gatherers")
                .GetChildren()
                .Select(gs => CreateGatherer(gs, scatterGatherConfiguration.Registry, sp))
                .ToList();

            var options = new ScatterGatherOptions
            {
                UseOutputFormatters = useOutputFormatters,
                Gatherers = gatherers
            };
            customize?.Invoke(template, options);
            result.Add(builder.MapScatterGather(template, options));
        }

        return result;
    }

    static IGatherer CreateGatherer(IConfigurationSection section, GathererFactoryRegistry registry, IServiceProvider sp)
    {
        var type = section["Type"];
        if (string.IsNullOrWhiteSpace(type))
        {
            return new HttpGatherer(section["Key"], section["DestinationUrl"]);
        }

        if (registry.TryGet(type, out var factory))
        {
            return factory(section, sp);
        }

        throw new InvalidOperationException(
            $"No gatherer factory registered for type '{type}'. " +
            $"Register one with services.AddScatterGather(config => config.AddGathererFactory(\"{type}\", ...)).");
    }
}
