using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ServiceComposer.AspNetCore;

sealed class GathererFactoryRegistry
{
    readonly Dictionary<string, Func<IConfigurationSection, IServiceProvider, IGatherer>> _factories
        = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string type, Func<IConfigurationSection, IServiceProvider, IGatherer> factory)
    {
        if (!_factories.TryAdd(type, factory))
        {
            throw new ArgumentException("Duplicate gatherer factory type: " + type);
        }
    }

    public bool TryGet(string type, out Func<IConfigurationSection, IServiceProvider, IGatherer> factory) => _factories.TryGetValue(type, out factory);
}
