using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;

namespace ServiceComposer.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static void AddViewModelComposition(this IServiceCollection services, IConfiguration configuration = null)
        {
            AddViewModelComposition(services, null, configuration);
        }

        public static void AddViewModelComposition(this IServiceCollection services, Action<ViewModelCompositionOptions> config, IConfiguration configuration = null)
        {
            var options = new ViewModelCompositionOptions(services, configuration);
            config?.Invoke(options);

            options.InitializeServiceCollection();
        }
    }
}
