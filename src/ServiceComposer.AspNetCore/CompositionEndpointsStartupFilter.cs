using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointDataSources
    {
        readonly List<CompositionEndpointDataSource> dataSources = [];

        public void Add(CompositionEndpointDataSource dataSource)
        {
            if (!dataSources.Contains(dataSource))
            {
                dataSources.Add(dataSource);
            }
        }

        public IReadOnlyList<CompositionEndpointDataSource> All => dataSources;
    }

    // Composition endpoints are created lazily, the first time routing reads
    // them, which usually happens at the first request. Once the application
    // has been fully configured, and every endpoint and convention is mapped,
    // build them, so that configuration errors are raised at startup.
    class CompositionEndpointsStartupFilter(CompositionEndpointDataSources compositionEndpointDataSources) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                foreach (var dataSource in compositionEndpointDataSources.All)
                {
                    _ = dataSource.Endpoints;
                }
            };
        }
    }
}
