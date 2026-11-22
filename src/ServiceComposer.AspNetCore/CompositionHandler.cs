using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public static partial class CompositionHandler
    {
        static async Task RunHandlerWithSpan(ICompositionRequestsHandler handler, HttpRequest request)
        {
            var handlerType = handler.GetType();
            Activity activity = null;

            if (CompositionTelemetry.ActivitySource.HasListeners())
            {
                activity = CompositionTelemetry.ActivitySource.StartActivity(CompositionTelemetry.Spans.Handler, ActivityKind.Internal);
                if (activity != null)
                {
                    activity.DisplayName = handlerType.FullName ?? handlerType.Name;
                    if (activity.IsAllDataRequested)
                    {
                        activity.SetTag(CompositionTelemetry.Tags.HandlerType, handlerType.FullName ?? handlerType.Name);
                        if (handlerType.Namespace != null)
                            activity.SetTag(CompositionTelemetry.Tags.HandlerNamespace, handlerType.Namespace);
                    }
                }
            }

            try
            {
                await handler.Handle(request);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                    activity.SetTag("otel.status_code", "error");
                    activity.SetTag("otel.status_description", ex.Message);
                    activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                    {
                        ["exception.type"] = ex.GetType().FullName ?? ex.GetType().Name,
                        ["exception.message"] = ex.Message,
                        ["exception.stacktrace"] = ex.ToString()
                    }));
                }
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        // Each handler runs behind its own filters: the attributes on its class, then the ones on
        // its method, then the ICompositionRequestFilter<T> registered for it. Not calling next
        // skips the handler only.
        static Task RunHandlerWithFilters(ICompositionRequestsHandler handler, HttpRequest request, CompositionMetadataRegistry metadataRegistry)
        {
            var filters = GetFilters(handler.GetType(), request.HttpContext.RequestServices, metadataRegistry);
            if (filters.Count == 0)
            {
                return RunHandlerWithSpan(handler, request);
            }

            CompositionRequestFilterDelegate pipeline = _ => new ValueTask(RunHandlerWithSpan(handler, request));
            for (var i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                var next = pipeline;
                pipeline = filterContext => filter.InvokeAsync(filterContext, next);
            }

            return pipeline(new CompositionRequestFilterContext(request.HttpContext, handler)).AsTask();
        }

        static List<ICompositionRequestFilter> GetFilters(Type handlerType, IServiceProvider services, CompositionMetadataRegistry metadataRegistry)
        {
            var filters = new List<ICompositionRequestFilter>();
            if (metadataRegistry.Components.TryGetValue(handlerType, out var metadata))
            {
                // The class' attributes come first, then the method's
                filters.AddRange(metadata.OfType<ICompositionRequestFilter>());
            }

            var typeBasedFilterType = typeof(ICompositionRequestFilter<>).MakeGenericType(handlerType);
            filters.AddRange(services.GetServices(typeBasedFilterType).OfType<ICompositionRequestFilter>());

            return filters;
        }

        // Walks up the factories: the route's endpoint scoped one, then the global one,
        // then the default dynamic view model. A factory returning null falls through.
        static object CreateViewModel(HttpContext context, CompositionContext compositionContext, Type[] componentsTypes)
        {
            var endpointScopedFactoryType = componentsTypes.SingleOrDefault(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t));
            if (endpointScopedFactoryType != null)
            {
                var endpointScopedFactory = (IViewModelFactory)context.RequestServices.GetRequiredService(endpointScopedFactoryType);
                var viewModel = endpointScopedFactory.CreateViewModel(context, compositionContext);
                if (viewModel != null)
                {
                    return viewModel;
                }

                LogFactoryFallThrough(context, endpointScopedFactoryType);
            }

            var globalFactory = context.RequestServices.GetService<IViewModelFactory>();
            if (globalFactory != null)
            {
                var viewModel = globalFactory.CreateViewModel(context, compositionContext);
                if (viewModel != null)
                {
                    return viewModel;
                }

                LogFactoryFallThrough(context, globalFactory.GetType());
            }

            return new ExpandoObject();
        }

        static void LogFactoryFallThrough(HttpContext context, Type factoryType)
        {
            var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(CompositionHandler));
            logger?.LogDebug("View model factory {ViewModelFactory} returned null for {Method} {Path}, falling back to the next factory.",
                factoryType.Name, context.Request.Method, context.Request.Path.Value);
        }

        internal static async Task<object> HandleComposableRequest(HttpContext context, CompositionContext compositionContext, Type[] componentsTypes)
        {
            var request = context.Request;

            var viewModel = CreateViewModel(context, compositionContext, componentsTypes);

            try
            {
                request.SetViewModel(viewModel);
                request.SetCompositionContext(compositionContext);

                await Task.WhenAll(context.RequestServices.GetServices<IViewModelPreviewHandler>()
                    .Select(visitor => visitor.Preview(request))
                    .ToList());

                var handlers = componentsTypes.Select(type => context.RequestServices.GetRequiredService(type)).ToArray();
                //TODO: if handlers == none we could shortcut to 404 here

                foreach (var subscriber in handlers.OfType<ICompositionEventsSubscriber>())
                {
                    subscriber.Subscribe(compositionContext);
                }

                //TODO: if handlers == none we could shortcut again to 404 here
                var metadataRegistry = context.RequestServices.GetRequiredService<CompositionMetadataRegistry>();
                var pending = handlers.OfType<ICompositionRequestsHandler>()
                    .Select(handler => RunHandlerWithFilters(handler, request, metadataRegistry))
                    .ToList();

                if (pending.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return null;
                }
                else
                {
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(CompositionHandler));
                        if (ex is ModelBindingBadRequestException)
                        {
                            // A client error, logged as minimal APIs log binding failures
                            logger?.LogDebug(ex, "Model binding failed for request {RequestId}.", compositionContext.RequestId);
                        }
                        else
                        {
                            logger?.LogError(ex, "Composition failed for request {RequestId}.", compositionContext.RequestId);
                        }

                        //TODO: refactor to Task.WhenAll
                        var errorHandlers = handlers.OfType<ICompositionErrorsHandler>();
                        foreach (var handler in errorHandlers)
                        {
                            await handler.OnRequestError(request, ex);
                        }

                        throw;
                    }
                }

                return viewModel;
            }
            finally
            {
                compositionContext.CleanupSubscribers();
            }
        }
    }
}