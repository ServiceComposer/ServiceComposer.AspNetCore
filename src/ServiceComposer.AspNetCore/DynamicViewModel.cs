using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    class DynamicViewModel : DynamicObject, IPublishCompositionEvents, ICompositionEventsPublisher
    {
        readonly string _requestId;
        readonly RouteData _routeData;
        readonly HttpRequest _httpRequest;
        readonly IDictionary<Type, List<EventHandler<object>>> _subscriptions = new Dictionary<Type, List<EventHandler<object>>>();
        readonly IDictionary<Type, List<CompositionEventHandler<object>>> _compositionEventsSubscriptions = new Dictionary<Type, List<CompositionEventHandler<object>>>();
        readonly IDictionary<string, object> _properties = new Dictionary<string, object>();

        public DynamicViewModel(string requestId, RouteData routeData, HttpRequest httpRequest)
        {
            this._requestId = requestId;
            this._routeData = routeData;
            this._httpRequest = httpRequest;
        }

        public void CleanupSubscribers() => _subscriptions.Clear();

        public void Subscribe<TEvent>(EventHandler<TEvent> handler)
        {
            if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<EventHandler<object>>();
                _subscriptions.Add(typeof(TEvent), handlers);
            }

            handlers.Add((requestId, pageViewModel, @event, routeData, query) => handler(requestId, pageViewModel, (TEvent) @event, routeData, query));
        }

        public void Subscribe<TEvent>(CompositionEventHandler<TEvent> handler)
        {
            if (!_compositionEventsSubscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<CompositionEventHandler<object>>();
                _compositionEventsSubscriptions.Add(typeof(TEvent), handlers);
            }

            handlers.Add((@event, request) => handler((TEvent) @event, request));
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) => _properties.TryGetValue(binder.Name, out result);

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _properties[binder.Name] = value;
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;

            if (binder.Name == "RaiseEvent")
            {
                result = RaiseEventImpl(args[0]);
                return true;
            }

            if (binder.Name == "Merge")
            {
                result = MergeImpl((IDictionary<string, object>) args[0]);
                return true;
            }

            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (var item in _properties.Keys)
            {
                yield return item;
            }

            yield return "RaiseEvent";
            yield return "Merge";
        }

        Task RaiseEventImpl(object @event)
        {
            if (_subscriptions.TryGetValue(@event.GetType(), out var handlers))
            {
                var tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    tasks.Add(handler.Invoke(_requestId, this, @event, _routeData, _httpRequest));
                }

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        DynamicViewModel MergeImpl(IDictionary<string, object> source)
        {
            foreach (var item in source)
            {
                _properties[item.Key] = item.Value;
            }

            return this;
        }
    }
}