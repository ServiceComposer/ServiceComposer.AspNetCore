#if NETCOREAPP3_1 || NET5_0

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
        public RoutePattern RoutePattern { get; set; }

        public int Order { get; set; }
        
        public ResponseCasing DefaultResponseCasing { get; }

        private readonly Dictionary<ResponseCasing, JsonSerializerSettings> casingToSettingsMappings = new();

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order, ResponseCasing defaultResponseCasing)
        {
            Validate(routePattern, componentsTypes);
            
            casingToSettingsMappings.Add(ResponseCasing.PascalCase, new JsonSerializerSettings());
            casingToSettingsMappings.Add(ResponseCasing.CamelCase, new JsonSerializerSettings() {ContractResolver = new CamelCasePropertyNamesContractResolver()});

            RoutePattern = routePattern;
            Order = order;
            DefaultResponseCasing = defaultResponseCasing;
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
            ResponseCasing casing = DefaultResponseCasing;
            if (context.Request.Headers.TryGetValue("Accept-Casing", out var requestedCasing))
            {
                switch (requestedCasing)
                {
                    case "casing/pascal":
                        casing = ResponseCasing.PascalCase;
                        break;
                    case "casing/camel":
                        casing = ResponseCasing.CamelCase;
                        break;
                    default:
                        throw new NotSupportedException(
                            $"Requested casing ({requestedCasing}) is not supported, " +
                            $"supported values are: 'casing/pascal' or 'casing/camel'.");
                }
            }

            return casingToSettingsMappings[casing];
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