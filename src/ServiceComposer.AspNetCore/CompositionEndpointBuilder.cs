using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    partial class CompositionEndpointBuilder : EndpointBuilder
    {
        readonly Dictionary<ResponseCasing, JsonSerializerOptions> casingToSettingsMappings = new();
        readonly RoutePattern routePattern;
        readonly ResponseCasing defaultResponseCasing;
        readonly Type[] componentsTypes;
        readonly bool useOutputFormatters;
        EndpointFilterDelegate cachedPipeline;

        public int Order { get; }
        public IList<(Type ComponentType, IList<object> Metadata)> ComponentsMetadata { get; } = new List<(Type ComponentType, IList<object> Metadata)>();

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order, ResponseCasing defaultResponseCasing, bool useOutputFormatters)
        {
            Validate(routePattern, componentsTypes);

            casingToSettingsMappings.Add(ResponseCasing.PascalCase, new JsonSerializerOptions());
            casingToSettingsMappings.Add(ResponseCasing.CamelCase, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            });

            this.routePattern = routePattern;
            Order = order;
            this.defaultResponseCasing = defaultResponseCasing;
            this.componentsTypes = componentsTypes;
            this.useOutputFormatters = useOutputFormatters;
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

        JsonSerializerOptions GetSettings(HttpContext context)
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
                        var logger = context.RequestServices.GetService<ILogger<CompositionEndpointBuilder>>();
                        logger?.LogWarning("Unsupported Accept-Casing header value {RequestedCasing}. Supported values are 'casing/pascal' and 'casing/camel'.", (string)requestedCasing);
                        throw new NotSupportedException(
                            $"Requested casing ({requestedCasing}) is not supported, " +
                            $"supported values are: 'casing/pascal' or 'casing/camel'.");
                }
            }

            JsonSerializerOptions customSettings = null;
            var customSettingProvider = context.RequestServices.GetService<Func<HttpRequest, JsonSerializerOptions>>();
            if (customSettingProvider != null)
            {
                customSettings = customSettingProvider(context.Request);
                if (customSettings != null && casing == ResponseCasing.CamelCase && customSettings.PropertyNamingPolicy != JsonNamingPolicy.CamelCase)
                {
                    throw new ArgumentException($"Current HttpRequest is requesting camel case serialization. The supplied custom " +
                                                $"settings are not using as PropertyNamingPolicy JsonNamingPolicy.CamelCase. Either " +
                                                $"configure custom settings to use JsonNamingPolicy.CamelCase as PropertyNamingPolicy " +
                                                $"or change the request casing to be pascal case.");
                }
            }

            return customSettings ?? casingToSettingsMappings[casing];
        }

        public override Endpoint Build()
        {
            RequestDelegate = async context =>
            {
                // We need the body to be seekable otherwise if more than one
                // composition handler tries to bind a model to the body
                // it'll fail and only the first one succeeds
                context.Request.EnableBuffering();
                
                var argumentsByComponent = await GetAllComponentsArguments(context);
                var flatArguments = argumentsByComponent
                    .SelectMany(kvp => kvp.Value)
                    .Select(arg=>arg.Value)
                    .ToArray();
                
                RequestDelegate composer = async composerHttpContext =>
                {
                    var requestId = composerHttpContext.EnsureRequestIdIsSetup();
                    var compositionContext = new CompositionContext
                    (
                        requestId,
                        composerHttpContext.Request,
                        composerHttpContext.RequestServices.GetRequiredService<CompositionMetadataRegistry>(),
                        argumentsByComponent
                    );
                    var logger = composerHttpContext.RequestServices.GetService<ILogger<CompositionEndpointBuilder>>();
                    logger?.LogDebug("Handling composition request at {Method} {Template} with {HandlerCount} handler(s).",
                        composerHttpContext.Request.Method, routePattern.RawText, componentsTypes.Length);
                    await CompositionHandler.HandleComposableRequest(composerHttpContext, compositionContext, componentsTypes);
                };
                var pipeline = cachedPipeline ?? BuildAndCacheEndpointFilterDelegatePipeline(composer, context.RequestServices);
                
                // TODO use source generators
                //   When we'll have convention-based handlers this could be
                //   source-generated to use the the most appropriate filter
                //   invocation context based on the number of arguments to bind
                EndpointFilterInvocationContext invocationContext = new DefaultEndpointFilterInvocationContext(context, flatArguments);
                var viewModel = await pipeline(invocationContext);
                
                if (viewModel != null)
                {
                    var containsActionResult = context.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey);
                    switch (useOutputFormatters)
                    {
                        case false when containsActionResult:
                            throw new NotSupportedException($"Setting an action result requires output formatters support. " +
                                                            $"Enable output formatters by setting to true the {nameof(ResponseSerializationOptions.UseOutputFormatters)} " +
                                                            $"configuration property of the {nameof(ResponseSerializationOptions)} instance.");
                        case true when containsActionResult:
                            await context.ExecuteResultAsync(context.Items[HttpRequestExtensions.ComposedActionResultKey] as IActionResult);
                            break;
                        case true:
                            await context.WriteModelAsync(viewModel);
                            break;
                        default:
                        {
                            var options = GetSettings(context);
                            var json = JsonSerializer.Serialize(viewModel, options);
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
