using Microsoft.Extensions.DependencyInjection;
using System;
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
using Microsoft.Extensions.Configuration;
#endif

namespace ServiceComposer.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
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
#else
        public static void AddViewModelComposition(this IServiceCollection services)
        {
            AddViewModelComposition(services, null);
        }
        
        public static void AddViewModelComposition(this IServiceCollection services, Action<ViewModelCompositionOptions> config)
        {
            var options = new ViewModelCompositionOptions(services);
            config?.Invoke(options);

            options.InitializeServiceCollection();
        }
#endif
    }
}
