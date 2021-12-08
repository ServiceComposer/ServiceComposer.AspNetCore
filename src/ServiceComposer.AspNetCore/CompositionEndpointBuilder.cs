using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointBuilder : EndpointBuilder
    {
        readonly Dictionary<ResponseCasing, JsonSerializerSettings> casingToSettingsMappings = new();
        readonly RoutePattern routePattern;
        readonly ResponseCasing defaultResponseCasing;
        
        public int Order { get; }

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order, ResponseCasing defaultResponseCasing, bool useOutputFormatters)
        {
            Validate(routePattern, componentsTypes);

            casingToSettingsMappings.Add(ResponseCasing.PascalCase, new JsonSerializerSettings());
            casingToSettingsMappings.Add(ResponseCasing.CamelCase, new JsonSerializerSettings() {ContractResolver = new CamelCasePropertyNamesContractResolver()});

            this.routePattern = routePattern;
            Order = order;
            this.defaultResponseCasing = defaultResponseCasing;
            RequestDelegate = async context =>
            {
                var viewModel = await CompositionHandler.HandleComposableRequest(context, componentsTypes);
                if (viewModel != null)
                {
                    var containsActionResult = context.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey);
                    switch (useOutputFormatters)
                    {
                        case false when containsActionResult:
                            throw new NotSupportedException($"Setting an action results requires output formatters supports. " +
                                                            $"Enable output formatters by setting to true the {nameof(ResponseSerializationOptions.UseOutputFormatters)} " +
                                                            $"configuration property in the {nameof(ResponseSerializationOptions)} options.");
                        case true when containsActionResult:
                            await context.ExecuteResultAsync(context.Items[HttpRequestExtensions.ComposedActionResultKey] as IActionResult);
                            break;
                        case true:
                            await context.WriteModelAsync(viewModel);
                            break;
                        default:
                        {
                            var json = JsonConvert.SerializeObject(viewModel, GetSettings(context));
                            context.Response.ContentType = "application/json; charset=utf-8";
                            await context.Response.WriteAsync(json);
                            break;
                        }
                    }
                }
                else
                {
                    await context.Response.WriteAsync(string.Empty);
                }
            };
        }

        static void Validate(RoutePattern routePattern, Type[] componentsTypes)
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
            var casing = defaultResponseCasing;
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
                routePattern,
                Order,
                new EndpointMetadataCollection(Metadata),
                DisplayName);

            return routeEndpoint;
        }
    }
}