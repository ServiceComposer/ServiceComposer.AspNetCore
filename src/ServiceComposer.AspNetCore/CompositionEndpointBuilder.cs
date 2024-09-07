using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
        readonly Type[] componentsTypes;
        readonly bool useOutputFormatters;
        EndpointFilterDelegate cachedPipeline;

        public int Order { get; }

        public CompositionEndpointBuilder(RoutePattern routePattern, Type[] componentsTypes, int order, ResponseCasing defaultResponseCasing, bool useOutputFormatters)
        {
            Validate(routePattern, componentsTypes);

            casingToSettingsMappings.Add(ResponseCasing.PascalCase, new JsonSerializerSettings());
            casingToSettingsMappings.Add(ResponseCasing.CamelCase, new JsonSerializerSettings() {ContractResolver = new CamelCasePropertyNamesContractResolver()});

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

        static object buildAndCacheEndpointFilterDelegatePipelineSyncLock = new ();
        EndpointFilterDelegate BuildAndCacheEndpointFilterDelegatePipeline(RequestDelegate composer, IServiceProvider serviceProvider)
        {
            lock (buildAndCacheEndpointFilterDelegatePipelineSyncLock)
            {
                var factoryContext = new EndpointFilterFactoryContext
                {
                    MethodInfo = composer.Method,
                    ApplicationServices = serviceProvider
                };

                EndpointFilterDelegate filteredInvocation = async context =>
                {
                    if (context.HttpContext.Response.StatusCode < 400)
                    {
                        await composer(context.HttpContext);
                        var viewModel = context.HttpContext.Request.GetComposedResponseModel();

                        var containsActionResult =
                            context.HttpContext.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey);
                        switch (useOutputFormatters)
                        {
                            case false when containsActionResult:
                                throw new NotSupportedException(
                                    $"Setting an action result requires output formatters supports. " +
                                    $"Enable output formatters by setting to true the {nameof(ResponseSerializationOptions.UseOutputFormatters)} " +
                                    $"configuration property in the {nameof(ResponseSerializationOptions)} options.");
                            case true when containsActionResult:
                                return context.HttpContext.Items[HttpRequestExtensions.ComposedActionResultKey] as
                                    IActionResult;
                        }

                        return viewModel;
                    }

                    return EmptyHttpResult.Instance;
                };

                var terminatorFilterDelegate = filteredInvocation;
                for (var i = FilterFactories.Count - 1; i >= 0; i--)
                {
                    var currentFilterFactory = FilterFactories[i];
                    filteredInvocation = currentFilterFactory(factoryContext, filteredInvocation);
                }

                cachedPipeline = ReferenceEquals(terminatorFilterDelegate, filteredInvocation)
                    ? terminatorFilterDelegate // The filter factories have run without modifications, skip running the pipeline.
                    : filteredInvocation;

                return cachedPipeline;
            }
        }

        public override Endpoint Build()
        {
            RequestDelegate = async context =>
            {
                RequestDelegate composer = async composerHttpContext => await CompositionHandler.HandleComposableRequest(composerHttpContext, componentsTypes);
                var pipeline = cachedPipeline ?? BuildAndCacheEndpointFilterDelegatePipeline(composer, context.RequestServices);
                
                EndpointFilterInvocationContext invocationContext = new DefaultEndpointFilterInvocationContext(context);
                var viewModel = await pipeline(invocationContext);
                
                if (viewModel != null)
                {
                    var containsActionResult = context.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey);
                    switch (useOutputFormatters)
                    {
                        case false when containsActionResult:
                            throw new NotSupportedException($"Setting an action result requires output formatters supports. " +
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
            
            var routeEndpoint = new RouteEndpoint(
                RequestDelegate,
                routePattern,
                Order,
                new EndpointMetadataCollection(Metadata),
                DisplayName);

            return routeEndpoint;
        }
        
#if NET8_0
        // static RequestDelegate Create(RequestDelegate requestDelegate, RequestDelegateFactoryOptions options)
        // {
        //     var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;
        //     var jsonOptions = serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions();
        //     var jsonSerializerOptions = jsonOptions.SerializerOptions;
        //
        //     var factoryContext = new EndpointFilterFactoryContext
        //     {
        //         MethodInfo = requestDelegate.Method,
        //         ApplicationServices = options.EndpointBuilder.ApplicationServices
        //     };
        //     var jsonTypeInfo = (JsonTypeInfo<object>)jsonSerializerOptions.GetReadOnlyTypeInfo(typeof(object));
        //
        //     EndpointFilterDelegate filteredInvocation = async (EndpointFilterInvocationContext context) =>
        //     {
        //         if (context.HttpContext.Response.StatusCode < 400)
        //         {
        //             await requestDelegate(context.HttpContext);
        //         }
        //         return EmptyHttpResult.Instance;
        //     };
        //
        //     var initialFilteredInvocation = filteredInvocation;
        //     for (var i = options.EndpointBuilder.FilterFactories.Count - 1; i >= 0; i--)
        //     {
        //         var currentFilterFactory = options.EndpointBuilder.FilterFactories[i];
        //         filteredInvocation = currentFilterFactory(factoryContext, filteredInvocation);
        //     }
        //
        //     // The filter factories have run without modifying per-request behavior, we can skip running the pipeline.
        //     if (ReferenceEquals(initialFilteredInvocation, filteredInvocation))
        //     {
        //         return requestDelegate;
        //     }
        //
        //     return async (HttpContext httpContext) =>
        //     {
        //         var obj = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, new object[] { httpContext }));
        //         if (obj is not null)
        //         {
        //             await ExecuteHandlerHelper.ExecuteReturnAsync(obj, httpContext, jsonTypeInfo);
        //         }
        //     };
        // }
#endif
    }
}