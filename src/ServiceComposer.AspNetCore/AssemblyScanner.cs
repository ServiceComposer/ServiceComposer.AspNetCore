using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

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

        readonly List<Func<string, FilterResults>> assemblyFilters = [];

        internal IEnumerable<Assembly> Scan(ILogger logger = null)
        {
            var assemblies = new Dictionary<string, Assembly>();

            bool FullPathsFilter(string fullPath)
            {
                return assemblyFilters.All(filter => filter(fullPath) == FilterResults.Include);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (FullPathsFilter(assembly.Location))
                {
                    assemblies.TryAdd(assembly.GetName().FullName, assembly);
                }
            }

            foreach (var patternToUse in assemblySearchPatternsToUse)
            {
                var assembliesFullPaths = Directory
                    .GetFiles(AppContext.BaseDirectory, patternToUse, DirectorySearchOptions)
                    .Where(FullPathsFilter);

                foreach (var assemblyFullPath in assembliesFullPaths)
                {
                    AssemblyValidator.ValidateAssemblyFile(assemblyFullPath, out var shouldLoad, out var reason);
                    if (shouldLoad)
                    {
                        try
                        {
                            var assembly = Assembly.LoadFrom(assemblyFullPath);
                            assemblies.TryAdd(assembly.GetName().FullName, assembly);
                        }
                        catch (FileLoadException)
                        {
                            logger?.LogDebug("Skipping {AssemblyPath}: already loaded in the current AppDomain.", assemblyFullPath);
                        }
                    }
                    else
                    {
                        logger?.LogDebug("Skipping {AssemblyPath}: {Reason}.", assemblyFullPath, reason);
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
                    AssemblyValidator.ValidateAssemblyFile(platformAssemblyFullPath, out var shouldLoad, out var reason);
                    if (shouldLoad)
                    {
                        try
                        {
                            var assembly = Assembly.LoadFrom(platformAssemblyFullPath);
                            assemblies.TryAdd(assembly.GetName().FullName, assembly);
                        }
                        catch (FileLoadException)
                        {
                            logger?.LogDebug("Skipping {AssemblyPath}: already loaded in the current AppDomain.", platformAssemblyFullPath);
                        }
                    }
                    else
                    {
                        logger?.LogDebug("Skipping {AssemblyPath}: {Reason}.", platformAssemblyFullPath, reason);
                    }
                }
            }

            return assemblies.Values;
        }

        public void AddAssemblyFilter(Func<string, FilterResults> filter)
        {
            assemblyFilters.Add(filter);
        }
    }
}
