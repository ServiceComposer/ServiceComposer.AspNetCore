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
        IReadOnlyList<Endpoint> cachedEndpoints;

        public void AddEndpointBuilder(CompositionEndpointBuilder endpointBuilder)
        {
            _endpointBuilders.Add(endpointBuilder);
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