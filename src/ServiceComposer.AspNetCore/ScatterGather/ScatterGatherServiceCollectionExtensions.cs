using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

public static class ScatterGatherServiceCollectionExtensions
{
    public static IServiceCollection AddScatterGatherer(this IServiceCollection services, Action<ScatterGatherConfiguration> configure = null)
    {
        var configuration = new ScatterGatherConfiguration();
        configure?.Invoke(configuration);
        services.AddSingleton(configuration);
        
        return services;
    }
}
