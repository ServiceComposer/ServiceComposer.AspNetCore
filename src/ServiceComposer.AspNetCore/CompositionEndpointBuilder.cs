#if NETCOREAPP3_1

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            RequestDelegate = async context =>
            {
                var viewModel = await CompositionHandler.HandleComposableRequest(context, _compositionHandlers);
                if (viewModel != null)
                {
                    var json = (string) JsonConvert.SerializeObject(viewModel, GetSettings(context));
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(json);
                }
                else
                {
                    await context.Response.WriteAsync(string.Empty);
                }
            };
        }

        JsonSerializerSettings GetSettings(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Accept-Casing", out StringValues casing))
            {
                casing = "casing/camel";
            }

            switch (casing)
            {
                case "casing/pascal":
                    return new JsonSerializerSettings();

                default: // "casing/camel":
                    return new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
            }
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

#endif