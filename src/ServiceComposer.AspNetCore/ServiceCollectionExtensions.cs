using Microsoft.Extensions.DependencyInjection;
using System;

namespace ServiceComposer.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static void AddViewModelComposition(this IServiceCollection services)
        {
            AddViewModelComposition(services, AppContext.BaseDirectory, null);
        }

        public static void AddViewModelComposition(this IServiceCollection services, string compositionsPath)
        {
            AddViewModelComposition(services, compositionsPath, null);
        }

        public static void AddViewModelComposition(this IServiceCollection services, Action<ViewModelCompositionOptions> config)
        {
            AddViewModelComposition(services, AppContext.BaseDirectory, config);
        }

        public static void AddViewModelComposition(this IServiceCollection services, string compositionsPath, Action<ViewModelCompositionOptions> config)
        {
            var options = new ViewModelCompositionOptions(services, compositionsPath);
            config?.Invoke(options);

            options.InitializeServiceCollection();
        }
    }
}
