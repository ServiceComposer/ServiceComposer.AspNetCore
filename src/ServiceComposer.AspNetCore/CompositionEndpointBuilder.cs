#if NETCOREAPP3_1 || NET5_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointBuilder : EndpointBuilder
    {
        private readonly bool useOutputFormatters;
        private readonly Dictionary<ResponseCasing, JsonSerializerSettings> casingToSettingsMappings = new();

        public RoutePattern RoutePattern { get; set; }

        public int Order { get; set; }

        public ResponseCasing DefaultResponseCasing { get; }

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order, ResponseCasing defaultResponseCasing, bool useOutputFormatters)
        {
            this.useOutputFormatters = useOutputFormatters;
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
                    if (useOutputFormatters)
                    {
                        if (context.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey))
                        {
                            await context.ExecuteResultAsync(context.Items[HttpRequestExtensions.ComposedActionResultKey] as IActionResult);
                        }
                        else
                        {
                            await context.WriteModelAsync(viewModel);
                        }
                    }
                    else
                    {
                        var json = (string)JsonConvert.SerializeObject(viewModel, GetSettings(context));
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(json);
                    }
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

            JsonSerializerSettings customSettings = null;
            var customSettingProvider = context.RequestServices.GetService<Func<HttpRequest, JsonSerializerSettings>>();
            if (customSettingProvider != null)
            {
                customSettings = customSettingProvider(context.Request);
                if (customSettings != null && casing == ResponseCasing.CamelCase && customSettings.ContractResolver is not CamelCasePropertyNamesContractResolver)
                {
                    throw new ArgumentException($"Current HttpRequest is requesting camel case serialization. " +
                                                $"The supplied custom settings are not using as ContractResolver an " +
                                                $"instance of {nameof(CamelCasePropertyNamesContractResolver)}. Either " +
                                                $"configure custom settings to use {nameof(CamelCasePropertyNamesContractResolver)} " +
                                                $"as contract resolver by setting the property ContractResolver, or change the request " +
                                                $"casing to be pascal case.");
                }
            }

            return customSettings ?? casingToSettingsMappings[casing];
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