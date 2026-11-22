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
using Microsoft.Extensions.Options;

namespace ServiceComposer.AspNetCore
{
    partial class CompositionEndpointBuilder : EndpointBuilder
    {
        // The cached endpoint filter pipeline is built once per endpoint. The
        // composer that runs at the bottom of the pipeline is per-request, so
        // we cannot capture it in the cached delegate's closure (the cached
        // delegate would replay the first request's bound arguments forever).
        // Stash the per-request composer in HttpContext.Items under this key
        // and have the innermost pipeline delegate read it back from there.
        internal const string CurrentComposerKey = "__ServiceComposer.CurrentComposer";

        readonly RoutePattern routePattern;
        readonly ResponseCasing? explicitResponseCasing;
        // Derived from the application's minimal APIs JSON options on the first request
        JsonSerializerOptions httpJsonSerializerOptions;
        JsonSerializerOptions pascalCaseSerializerOptions;
        JsonSerializerOptions camelCaseSerializerOptions;
        readonly Type[] componentsTypes;
        readonly bool useOutputFormatters;
        EndpointFilterDelegate cachedPipeline;

        public int Order { get; }
        public IList<(Type ComponentType, IList<object> Metadata)> ComponentsMetadata { get; } = new List<(Type ComponentType, IList<object> Metadata)>();

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order, ResponseCasing? explicitResponseCasing, bool useOutputFormatters)
        {
            Validate(routePattern, componentsTypes);

            this.routePattern = routePattern;
            Order = order;
            this.explicitResponseCasing = explicitResponseCasing;
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
            var casing = explicitResponseCasing;
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

            var customSettingProvider = context.RequestServices.GetService<Func<HttpRequest, JsonSerializerOptions>>();
            var customSettings = customSettingProvider?.Invoke(context.Request);
            if (customSettings != null)
            {
                if ((casing ?? ResponseCasing.CamelCase) == ResponseCasing.CamelCase && customSettings.PropertyNamingPolicy != JsonNamingPolicy.CamelCase)
                {
                    throw new ArgumentException($"Current HttpRequest is requesting camel case serialization. The supplied custom " +
                                                $"settings are not using as PropertyNamingPolicy JsonNamingPolicy.CamelCase. Either " +
                                                $"configure custom settings to use JsonNamingPolicy.CamelCase as PropertyNamingPolicy " +
                                                $"or change the request casing to be pascal case.");
                }

                return customSettings;
            }

            // An explicit casing only replaces the naming policies: the rest of the
            // application's JSON options, e.g. converters, still apply
            return casing switch
            {
                ResponseCasing.PascalCase => pascalCaseSerializerOptions ??= WithNamingPolicy(GetHttpJsonSerializerOptions(context), null),
                ResponseCasing.CamelCase => camelCaseSerializerOptions ??= WithNamingPolicy(GetHttpJsonSerializerOptions(context), JsonNamingPolicy.CamelCase),
                _ => httpJsonSerializerOptions ??= WithDictionaryKeyPolicyFallback(GetHttpJsonSerializerOptions(context))
            };
        }

        // The options configured with ConfigureHttpJsonOptions(), as for minimal APIs
        static JsonSerializerOptions GetHttpJsonSerializerOptions(HttpContext context)
        {
            return context.RequestServices.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions
                   ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        static JsonSerializerOptions WithNamingPolicy(JsonSerializerOptions source, JsonNamingPolicy namingPolicy)
        {
            return new JsonSerializerOptions(source)
            {
                PropertyNamingPolicy = namingPolicy,
                DictionaryKeyPolicy = namingPolicy
            };
        }

        // The dynamic view model is an ExpandoObject, serialized as a dictionary: without a
        // dictionary key policy, its members wouldn't follow the property naming policy
        static JsonSerializerOptions WithDictionaryKeyPolicyFallback(JsonSerializerOptions source)
        {
            if (source.DictionaryKeyPolicy != null || source.PropertyNamingPolicy == null)
            {
                return source;
            }

            return new JsonSerializerOptions(source)
            {
                DictionaryKeyPolicy = source.PropertyNamingPolicy
            };
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
                // The cached pipeline reads the composer back from HttpContext.Items, so the
                // per-request composer (and its captured argumentsByComponent) is the one
                // that runs - not whichever composer happened to be the first one built.
                context.Items[CurrentComposerKey] = composer;
                var pipeline = cachedPipeline ?? BuildAndCacheEndpointFilterDelegatePipeline(context.RequestServices);
                
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
