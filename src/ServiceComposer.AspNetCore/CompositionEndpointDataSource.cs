using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointDataSource : EndpointDataSource, IEndpointConventionBuilder
    {
        readonly object endpointsBuildLock = new();
        readonly List<Action<EndpointBuilder>> conventions = [];
        readonly List<CompositionEndpointBuilder> _endpointBuilders = [];
        readonly List<Action> deferredMappings = [];
        IReadOnlyList<Endpoint> cachedEndpoints;

        public void AddEndpointBuilder(CompositionEndpointBuilder endpointBuilder)
        {
            _endpointBuilders.Add(endpointBuilder);
        }

        // Mapping composition handlers can require reading the endpoints of
        // other data sources, e.g. to compose over controllers. Reading them
        // builds them, and conventions added to those data sources afterward
        // are ignored. Deferred mappings run the first time Endpoints is read,
        // when routing builds its matcher, after the application finished
        // mapping endpoints and adding conventions.
        public void AddDeferredMapping(Action mapping)
        {
            deferredMappings.Add(mapping);
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        // ASP.NET Core's routing infrastructure can, and does, enumerate an
        // EndpointDataSource's Endpoints more than once during startup and
        // routing setup. Since GetChangeToken() above never signals a change,
        // there's never a reason to rebuild: conventions (e.g. AddEndpointFilter)
        // must be applied to each endpoint builder exactly once, otherwise
        // conventions that mutate state - like appending an endpoint filter
        // factory - get re-applied on every enumeration, stacking duplicates
        // onto the same, reused CompositionEndpointBuilder instances.
        public override IReadOnlyList<Endpoint> Endpoints
        {
            get
            {
                if (cachedEndpoints == null)
                {
                    lock (endpointsBuildLock)
                    {
                        if (cachedEndpoints == null)
                        {
                            foreach (var mapping in deferredMappings)
                            {
                                mapping();
                            }
                            deferredMappings.Clear();
                        }

                        cachedEndpoints ??= _endpointBuilders
                            .OrderBy(builder => builder.Order)
                            .Select(builder =>
                            {
                                foreach (var convention in conventions)
                                {
                                    convention(builder);
                                }
                                return builder.Build();
                            }).ToArray();
                    }
                }

                return cachedEndpoints;
            }
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            conventions.Add(convention);
        }
    }
}