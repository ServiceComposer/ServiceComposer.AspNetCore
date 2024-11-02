using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    class CompositionContext(string requestId, HttpRequest httpRequest, CompositionMetadataRegistry metadataRegistry)
        : ICompositionContext, ICompositionEventsPublisher
    {
        readonly ConcurrentDictionary<Type, List<CompositionEventHandler<object>>> _compositionEventsSubscriptions = new();

        public string RequestId { get; } = requestId;

        public Task RaiseEvent<TEvent>(TEvent @event)
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
            if (_compositionEventsSubscriptions.TryGetValue(@event.GetType(), out var compositionHandlers))
            {
                handlers.AddRange(compositionHandlers.Cast<CompositionEventHandler<TEvent>>());
            }
            
            var tasks = handlers.Select(handler => handler(@event, httpRequest)).ToList();
            return Task.WhenAll(tasks);
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