using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    class DynamicViewModel : DynamicObject, IPublishCompositionEvents, ICompositionEventsPublisher, ICompositionContext
    {
        readonly ILogger<DynamicViewModel> _logger;
        readonly CompositionContext _compositionContext;
        readonly ConcurrentDictionary<string, object> _properties = new();
        
        public DynamicViewModel(ILogger<DynamicViewModel> logger, CompositionContext compositionContext)
        {
            _logger = logger;
            _compositionContext = compositionContext;
            _compositionContext.CurrentViewModel = this;
        }

        public void Subscribe<TEvent>(EventHandler<TEvent> handler)
        {
            _compositionContext.Subscribe(handler); 
        }

        public void Subscribe<TEvent>(CompositionEventHandler<TEvent> handler)
        {
            _compositionContext.Subscribe(handler);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) => _properties.TryGetValue(binder.Name, out result);

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _properties.AddOrUpdate(binder.Name, value, (key, existingValue) => value);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;

            switch (binder.Name)
            {
                case "RaiseEvent":
                    _logger.LogWarning(message: "dynamic.RaiseEvent is obsolete. It'll be treated as an error starting v2 and removed in v3. Use HttpRequest.GetCompositionContext() to raise events.");
                    result = RaiseEventImpl(args[0]);
                    return true;
                case "Merge":
                    result = MergeImpl((IDictionary<string, object>) args[0]);
                    return true;
                default:
                    return false;
            }
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
            return _compositionContext.RaiseEvent(@event);
        }

        DynamicViewModel MergeImpl(IDictionary<string, object> source)
        {
            foreach (var item in source)
            {
                _properties[item.Key] = item.Value;
            }

            return this;
        }

        Task ICompositionContext.RaiseEvent(object @event)
        {
            return _compositionContext.RaiseEvent(@event);
        }

        string ICompositionContext.RequestId
        {
            get { return _compositionContext.RequestId; }
        }
    }
}