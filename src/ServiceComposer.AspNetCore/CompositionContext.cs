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

        public Task RaiseEvent(object @event)
        {
            var handlers = new List<CompositionEventHandler<object>>();
            if (metadataRegistry.EventHandlers.TryGetValue(@event.GetType(), out var handlerTypes))
            {
                handlerTypes.ForEach(handlerType =>
                {
                    Task EventHandler(object evt, HttpRequest req)
                    {
                        var handler = httpRequest.HttpContext.RequestServices.GetRequiredService(handlerType);
                        var mi = handlerType.GetMethod(nameof(ICompositionEventsHandler<object>.Handle));
                        var ret = mi.Invoke(handler, new[] { @event, req });
                        return ret as Task;
                    }

                    handlers.Add(EventHandler);
                });
            }

            if (_compositionEventsSubscriptions.TryGetValue(@event.GetType(), out var compositionHandlers))
            {
                handlers.AddRange(compositionHandlers);
            }
            
            var tasks = handlers.Select(handler => handler.Invoke(@event, httpRequest)).ToList();
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