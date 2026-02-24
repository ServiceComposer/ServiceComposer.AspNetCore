using System;
using Microsoft.Extensions.Configuration;

namespace ServiceComposer.AspNetCore;

public class ScatterGatherConfiguration
{
    internal GathererFactoryRegistry Registry { get; } = new();
    
    /// <summary>
    /// Registers a factory for creating <see cref="IGatherer"/> instances from configuration
    /// when the gatherer entry has a <c>Type</c> field matching <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The discriminator value that must appear in the gatherer's <c>Type</c> configuration field.
    /// Matching is case-insensitive.
    /// </param>
    /// <param name="factory">
    /// A delegate that receives the gatherer's <see cref="IConfigurationSection"/> and the
    /// application's root <see cref="IServiceProvider"/>, and returns the <see cref="IGatherer"/>
    /// instance to use. The factory is invoked once at startup, not per request; the
    /// <see cref="IServiceProvider"/> is the singleton root provider. Do not resolve scoped
    /// services (e.g. <c>DbContext</c>) here — instead resolve them inside
    /// <see cref="IGatherer.Gather"/> via <c>HttpContext.RequestServices</c>.
    /// </param>
    public void AddGathererFactory(string type, Func<IConfigurationSection, IServiceProvider, IGatherer> factory)
    {
        Registry.Register(type, factory);
    }

    internal void RegisterKnownGatherers()
    {
        AddGathererFactory("http", (section, provider) => new HttpGatherer(section["Key"], section["DestinationUrl"])
        {
            IgnoreDownstreamRequestErrors = section.GetValue<bool>("IgnoreDownstreamRequestErrors")
        });
    }
}