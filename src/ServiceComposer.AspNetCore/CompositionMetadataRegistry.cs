using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    class CompositionMetadataRegistry
    {
        internal HashSet<Type> Components { get; } = [];
        internal Dictionary<Type, List<Type>> EventHandlers { get; } = new();

        public void AddComponent(Type type)
        {
            Components.Add(type);
        }

        public void AddEventHandler(Type eventType, Type handlerType)
        {
            if (!EventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                EventHandlers.Add(eventType, handlers);
            }
            
            handlers.Add(handlerType);
        }
    }
}