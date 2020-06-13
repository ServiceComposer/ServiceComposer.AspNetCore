using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointBuilder : EndpointBuilder
    {
        private readonly Type[] _compositionHandlers;
        public RoutePattern RoutePattern { get; set; }

        public int Order { get; set; }

        public CompositionEndpointBuilder(RoutePattern routePattern, IEnumerable<Type> compositionHandlers, int order)
        {
            _compositionHandlers = compositionHandlers.ToArray();
            RoutePattern = routePattern;
            Order = order;
            RequestDelegate = context =>
            {
                var types = _compositionHandlers;
                context.RequestServices.GetRequiredService(types[0]);

                return Task.CompletedTask;
            };
        }

        public override Endpoint Build()
        {
            var routeEndpoint = new RouteEndpoint(
                RequestDelegate,
                RoutePattern,
                Order,
                new EndpointMetadataCollection(Metadata),
                DisplayName);

            return routeEndpoint;
        }
    }
}