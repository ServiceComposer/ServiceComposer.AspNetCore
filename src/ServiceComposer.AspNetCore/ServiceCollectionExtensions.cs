using Microsoft.Extensions.DependencyInjection;
using System;

namespace ServiceComposer.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
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
    }
}
