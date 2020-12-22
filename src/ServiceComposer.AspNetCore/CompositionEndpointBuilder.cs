#if NETCOREAPP3_1 || NET5_0

using System;
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
        public RoutePattern RoutePattern { get; set; }

        public int Order { get; set; }

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order)
        {
            Validate(routePattern, componentsTypes);

            RoutePattern = routePattern;
            Order = order;
            RequestDelegate = async context =>
            {
                var viewModel = await CompositionHandler.HandleComposableRequest(context, componentsTypes);
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

        private void Validate(RoutePattern routePattern, Type[] componentsTypes)
        {
            var endpointScopedViewModelFactoriesCount = componentsTypes.Count(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t));
            if (endpointScopedViewModelFactoriesCount > 1)
            {
                var message = $"Only one {nameof(IEndpointScopedViewModelFactory)} is allowed per endpoint." +
                              $" Endpoint '{routePattern}' is bound to more than one view model factory.";
                throw new NotSupportedException(message);
            }
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