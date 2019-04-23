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
            if (!options.IsAssemblyScanningDisabled)
            {
                var fileNames = Directory.GetFiles(AppContext.BaseDirectory);
                var types = new List<Type>();
                foreach (var fileName in fileNames)
                {
                    types.AddRange(Assembly.LoadFrom(fileName).GetTypesFromAssembly());
                }

                var platformAssembliesString = (string)AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
                if (platformAssembliesString != null)
                {
                    var platformAssemblies = platformAssembliesString.Split(Path.PathSeparator);
                    foreach (var platformAssembly in platformAssemblies)
                    {
                        types.AddRange(Assembly.LoadFrom(platformAssembly).GetTypesFromAssembly());
                    }
                }

                foreach (var type in types)
                {
                    options.RegisterRouteInterceptor(type);
                }
            }
        }

        static IEnumerable<Type> GetTypesFromAssembly(this Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t =>
                {
                    var typeInfo = t.GetTypeInfo();
                    return !typeInfo.IsInterface
                        && !typeInfo.IsAbstract
                        && typeof(IInterceptRoutes).IsAssignableFrom(t);
                });

            return types;
        }
    }
}
