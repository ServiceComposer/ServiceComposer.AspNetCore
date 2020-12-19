using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ServiceComposer.AspNetCore
{
    class CompositionContext : ICompositionContext, IPublishCompositionEvents, ICompositionEventsPublisher
    {
        readonly RouteData _routeData;
        readonly HttpRequest _httpRequest;
        readonly ConcurrentDictionary<Type, List<EventHandler<object>>> _subscriptions = new();
        readonly ConcurrentDictionary<Type, List<CompositionEventHandler<object>>> _compositionEventsSubscriptions = new();
        
        public CompositionContext(string requestId, RouteData routeData, HttpRequest httpRequest)
        {
            _routeData = routeData;
            _httpRequest = httpRequest;
            RequestId = requestId;
        }

        //TODO: Remove once old style is dropped
        internal dynamic CurrentViewModel { get; set; }
        
        public string RequestId { get; }
        public Task RaiseEvent(object @event)
        {
            if (_subscriptions.TryGetValue(@event.GetType(), out var handlers))
            {
                //TODO: Remove once old style is dropped
                var tasks = handlers.Select(handler => (Task)handler.Invoke(RequestId, CurrentViewModel, @event, _routeData, _httpRequest)).ToList();

                return Task.WhenAll(tasks);
            }

            if (_compositionEventsSubscriptions.TryGetValue(@event.GetType(), out var compositionHandlers))
            {
                var tasks = compositionHandlers.Select(handler => handler.Invoke(@event, _httpRequest)).ToList();

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }
        
        public void Subscribe<TEvent>(EventHandler<TEvent> handler)
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<EventHandler<object>>();
                _subscriptions.TryAdd(typeof(TEvent), handlers);
            }

            handlers.Add((requestId, pageViewModel, @event, routeData, query) => handler(requestId, pageViewModel, (TEvent) @event, routeData, query));
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
            _subscriptions.Clear();
            _compositionEventsSubscriptions.Clear();
        } 
    }
}