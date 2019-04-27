using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

            if (options.AssemblyScanner.IsEnabled)
            {
                options.AssemblyScanner.AddTypesScanner(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                            && !typeInfo.IsAbstract
                            && typeof(IInterceptRoutes).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            options.RegisterRouteInterceptor(type);
                        }
                    });

                options.AssemblyScanner.ScanAndRegisterTypes();
            }
        }
    }
}
