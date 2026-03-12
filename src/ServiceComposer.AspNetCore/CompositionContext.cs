#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    class CompositionContext(
        string requestId,
        HttpRequest httpRequest,
        CompositionMetadataRegistry metadataRegistry,
        IDictionary<Type, IList<ModelBindingArgument>> componentsArguments)
        : ICompositionContext, ICompositionEventsPublisher
    {
        readonly ConcurrentDictionary<Type, List<CompositionEventHandler<object>>> _compositionEventsSubscriptions = new();

        public string RequestId { get; } = requestId;

        public async Task RaiseEvent<TEvent>(TEvent @event)
        {
            var handlers = new List<CompositionEventHandler<TEvent>>();
            if (metadataRegistry.EventHandlers.TryGetValue(typeof(TEvent), out var handlerTypes))
            {
                handlerTypes.ForEach(handlerType =>
                {
                    Task EventHandler(TEvent evt, HttpRequest req)
                    {
                        var handler = (ICompositionEventsHandler<TEvent>)httpRequest.HttpContext.RequestServices.GetRequiredService(handlerType);
                        return handler.Handle(@event, req);
                    }

                    handlers.Add(EventHandler);
                });
            }

            // not using typeof(TEvent) to prevent introducing a breaking change
            // due to the introduction of the generic TEvent parameter
            if (_compositionEventsSubscriptions.TryGetValue(@event!.GetType(), out var compositionHandlers))
            {
                handlers.AddRange(compositionHandlers.Cast<CompositionEventHandler<TEvent>>());
            }

            var logger = httpRequest.HttpContext.RequestServices.GetService<ILogger<CompositionContext>>();
            logger?.LogDebug("Raising event {EventType} to {HandlerCount} handler(s).", typeof(TEvent).Name, handlers.Count);

            var eventType = typeof(TEvent);
            Activity? activity = null;

            if (CompositionTelemetry.ActivitySource.HasListeners())
            {
                activity = CompositionTelemetry.ActivitySource.StartActivity("composition.event");
                if (activity != null)
                {
                    activity.DisplayName = eventType.FullName ?? eventType.Name;
                    if (activity.IsAllDataRequested)
                    {
                        activity.SetTag("composition.event.type", eventType.FullName ?? eventType.Name);
                        activity.SetTag("composition.event.namespace", eventType.Namespace);
                    }
                }
            }

            var tasks = handlers.Select(handler => handler(@event, httpRequest)).ToList();
            try
            {
                await Task.WhenAll(tasks);
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

        public IList<ModelBindingArgument>? GetArguments(ICompositionRequestsHandler owner) => GetArguments(owner.GetType());
        public IList<ModelBindingArgument>? GetArguments(ICompositionEventsSubscriber owner) => GetArguments(owner.GetType());
        public IList<ModelBindingArgument>? GetArguments<T>(ICompositionEventsHandler<T> owner) => GetArguments(owner.GetType());

        IList<ModelBindingArgument>? GetArguments(Type owningComponentType)
        {
            componentsArguments.TryGetValue(owningComponentType, out var arguments);
            return arguments;
        }

        public void Subscribe<TEvent>(CompositionEventHandler<TEvent> handler)
        {
            if (!_compositionEventsSubscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = [];
                _compositionEventsSubscriptions.TryAdd(typeof(TEvent), handlers);
            }

            handlers.Add((@event, request) => handler((TEvent) @event, request));
        }

        public void CleanupSubscribers()
        {
            _compositionEventsSubscriptions.Clear();
        }
    }
}