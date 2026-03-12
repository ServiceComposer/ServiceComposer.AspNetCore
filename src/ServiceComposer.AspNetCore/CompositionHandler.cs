using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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

        internal static async Task<object> HandleComposableRequest(HttpContext context, CompositionContext compositionContext, Type[] componentsTypes)
        {
            var request = context.Request;

            var factoryType = componentsTypes.SingleOrDefault(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t)) ?? typeof(IViewModelFactory);
            var viewModelFactory = (IViewModelFactory)context.RequestServices.GetService(factoryType); 
            var viewModel = viewModelFactory != null ? viewModelFactory.CreateViewModel(context, compositionContext) : new ExpandoObject();

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
                var pending = handlers.OfType<ICompositionRequestsHandler>()
                    .Select(handler =>
                    {
                        // TODO: apply composition filter here not before
                        // invoking the whole composition process
                        return RunHandlerWithSpan(handler, request);
                    })
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
                        logger?.LogError(ex, "Composition failed for request {RequestId}.", compositionContext.RequestId);

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