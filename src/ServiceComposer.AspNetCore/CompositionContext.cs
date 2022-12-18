using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ServiceComposer.AspNetCore
{
    class CompositionContext : ICompositionContext, ICompositionEventsPublisher
    {
        readonly RouteData _routeData;
        readonly HttpRequest _httpRequest;
        readonly ConcurrentDictionary<Type, List<CompositionEventHandler<object>>> _compositionEventsSubscriptions = new();

        public CompositionContext(string requestId, RouteData routeData, HttpRequest httpRequest)
        {
            _routeData = routeData;
            _httpRequest = httpRequest;
            RequestId = requestId;
        }
        public string RequestId { get; }
        public Task RaiseEvent(object @event)
        {
            if (_compositionEventsSubscriptions.TryGetValue(@event.GetType(), out var compositionHandlers))
            {
                var tasks = compositionHandlers.Select(handler => handler.Invoke(@event, _httpRequest)).ToList();

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(CompositionEventHandler<TEvent> handler)
        {
            if (!_compositionEventsSubscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<CompositionEventHandler<object>>();
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