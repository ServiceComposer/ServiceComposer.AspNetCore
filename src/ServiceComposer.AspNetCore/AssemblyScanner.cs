using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ServiceComposer.AspNetCore
{
    public class AssemblyScanner
    {
        static string[] assemblySearchPatternsToUse =
        {
            "*.dll",
            "*.exe"
        };

        internal AssemblyScanner()
        {

        }

        List<(Predicate<Type> Filter, Action<IEnumerable<Type>> RegistrationHandler)> typeFilters = new List<(Predicate<Type>, Action<IEnumerable<Type>>)>();

        public bool IsEnabled { get; private set; } = true;
        public void Disable()
        {
            IsEnabled = false;
        }

        internal void ScanAndRegisterTypes()
        {
            var assemblies = new List<Assembly>();
            foreach (var patternToUse in assemblySearchPatternsToUse)
            {
                var fileNames = Directory.GetFiles(AppContext.BaseDirectory, patternToUse);
                foreach (var fileName in fileNames)
                {
                    AssemblyValidator.ValidateAssemblyFile(fileName, out var shouldLoad, out var reason);
                    if (shouldLoad)
                    {
                        assemblies.Add(Assembly.LoadFrom(fileName));
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
                        assemblies.Add(Assembly.LoadFrom(platformAssembly));
                    }
                }
            }

            var allAssembliesAllTypes = assemblies.SelectMany(a => a.GetTypes());
            foreach (var filter in typeFilters)
            {
                var filteredTypes = allAssembliesAllTypes.Where(t => filter.Filter(t)).Distinct();
                filter.RegistrationHandler(filteredTypes);
            }
        }

        public void RegisterTypeFilter(Predicate<Type> filter, Action<IEnumerable<Type>> registrationHandler)
        {
            typeFilters.Add((filter, registrationHandler));
        }
    }
}
