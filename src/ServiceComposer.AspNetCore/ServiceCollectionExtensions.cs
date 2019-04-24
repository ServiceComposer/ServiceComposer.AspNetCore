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
        static string[] assemblySearchPatternsToUse =
        {
            "*.dll",
            "*.exe"
        };

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
                var types = new HashSet<Type>();
                foreach (var patternToUse in assemblySearchPatternsToUse)
                {
                    var fileNames = Directory.GetFiles(AppContext.BaseDirectory, patternToUse);
                    foreach (var fileName in fileNames)
                    {
                        AssemblyValidator.ValidateAssemblyFile(fileName, out var shouldLoad, out var reason);
                        if (shouldLoad)
                        {
                            var matchingTypes = Assembly.LoadFrom(fileName).GetTypesFromAssembly();
                            foreach (var type in matchingTypes)
                            {
                                if (!types.Contains(type))
                                {
                                    types.Add(type);
                                }
                            }
                        }
                    }
                }

                var platformAssembliesString = (string)AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
                if (platformAssembliesString != null)
                {
                    var platformAssemblies = platformAssembliesString.Split(Path.PathSeparator);
                    foreach (var platformAssembly in platformAssemblies)
                    {
                        AssemblyValidator.ValidateAssemblyFile(platformAssembly, out var shouldLoad, out var reason);
                        if (shouldLoad)
                        {
                            var matchingTypes = Assembly.LoadFrom(platformAssembly).GetTypesFromAssembly();
                            foreach (var type in matchingTypes)
                            {
                                if (!types.Contains(type))
                                {
                                    types.Add(type);
                                }
                            }
                        }
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
