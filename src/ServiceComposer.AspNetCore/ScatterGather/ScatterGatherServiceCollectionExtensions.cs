using System;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

public static class ScatterGatherServiceCollectionExtensions
{
    public static IServiceCollection AddScatterGather(this IServiceCollection services, Action<ScatterGatherConfiguration> configure = null)
    {
        var configuration = new ScatterGatherConfiguration();
        configuration.RegisterKnownGatherers();
        configure?.Invoke(configuration);
        services.AddSingleton(configuration);
        
        return services;
    }
}
