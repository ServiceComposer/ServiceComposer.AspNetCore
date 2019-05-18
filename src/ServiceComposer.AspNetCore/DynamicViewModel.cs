using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    class DynamicViewModel : DynamicObject, IPublishCompositionEvents
    {
        private readonly string requestId;
        private readonly RouteData routeData;
        private readonly HttpRequest httpRequest;
        private readonly IDictionary<Type, List<EventHandler<object>>> subscriptions = new Dictionary<Type, List<EventHandler<object>>>();
        private readonly IDictionary<string, object> properties = new Dictionary<string, object>();

        public DynamicViewModel(string requestId, RouteData routeData, HttpRequest httpRequest)
        {
            this.requestId = requestId;
            this.routeData = routeData;
            this.httpRequest = httpRequest;
        }

        public void CleanupSubscribers() => subscriptions.Clear();

        public void Subscribe<TEvent>(EventHandler<TEvent> handler)
        {
            if (!subscriptions.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<EventHandler<object>>();
                subscriptions.Add(typeof(TEvent), handlers);
            }

            handlers.Add((requestId, pageViewModel, @event, routeData, query) => handler(requestId, pageViewModel, (TEvent)@event, routeData, query));
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) => properties.TryGetValue(binder.Name, out result);

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            properties[binder.Name] = value;
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;

            if (binder.Name == "RaiseEvent")
            {
                result = this.RaiseEvent(args[0]);
                return true;
            }

            return false;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            foreach (var item in properties.Keys)
            {
                yield return item;
            }

            yield return "RaiseEvent";
        }

        public Task RaiseEvent(object @event)
        {
            if (subscriptions.TryGetValue(@event.GetType(), out var handlers))
            {
                var tasks = new List<Task>();
                foreach (var handler in handlers)
                {
                    tasks.Add(handler.Invoke(requestId, this, @event, routeData, httpRequest));
                }

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }
    }
}
