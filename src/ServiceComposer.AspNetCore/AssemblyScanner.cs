using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ServiceComposer.AspNetCore
{
    public class AssemblyScanner
    {
        public enum FilterResults
        {
            Exclude,
            Include
        }

        static readonly string[] assemblySearchPatternsToUse =
        {
            "*.dll",
            "*.exe"
        };

        internal AssemblyScanner()
        {

        }

        public SearchOption DirectorySearchOptions { get; set; } = SearchOption.TopDirectoryOnly;

        public bool IsEnabled { get; private set; } = true;
        public void Disable()
        {
            IsEnabled = false;
        }

        readonly List<Func<string, FilterResults>> assemblyFilters = new List<Func<string, FilterResults>>();

        internal IEnumerable<Assembly> Scan()
        {
            var assemblies = new List<Assembly>();

            bool FullPathsFilter(string fullPath)
            {
                return assemblyFilters.All(filter => filter(fullPath) == FilterResults.Include);
            }

            foreach (var patternToUse in assemblySearchPatternsToUse)
            {
                var assembliesFullPaths = Directory
                    .GetFiles(AppContext.BaseDirectory, patternToUse, DirectorySearchOptions)
                    .Where(FullPathsFilter);

                foreach (var assemblyFullPath in assembliesFullPaths)
                {
                    AssemblyValidator.ValidateAssemblyFile(assemblyFullPath, out var shouldLoad, out _);
                    if (shouldLoad)
                    {
                        assemblies.Add(Assembly.LoadFrom(assemblyFullPath));
                    }
                }
            }

            var platformAssembliesString = (string)AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            if (platformAssembliesString != null)
            {
                var platformAssembliesFullPaths = platformAssembliesString
                    .Split(Path.PathSeparator)
                    .Where((Func<string, bool>)FullPathsFilter);

                foreach (var platformAssemblyFullPath in platformAssembliesFullPaths)
                {
                    AssemblyValidator.ValidateAssemblyFile(platformAssemblyFullPath, out var shouldLoad, out _);
                    if (shouldLoad)
                    {
                        assemblies.Add(Assembly.LoadFrom(platformAssemblyFullPath));
                    }
                }
            }

            return assemblies.Distinct();
        }

        public void AddAssemblyFilter(Func<string, FilterResults> filter)
        {
            assemblyFilters.Add(filter);
        }
    }
}
