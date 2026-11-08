using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    class CompositionMetadataRegistry
    {
        readonly Dictionary<Type, IReadOnlyList<object>> components = new();

        // Each component is registered with the endpoint metadata of its
        // composition method (e.g., Http* attributes, model binding attributes).
        // Metadata is captured at registration time so that mapping endpoints
        // does not need to inspect component types through reflection.
        internal IReadOnlyDictionary<Type, IReadOnlyList<object>> Components => components;
        internal Dictionary<Type, List<Type>> EventHandlers { get; } = new();

        public void AddComponent(Type type, IReadOnlyList<object> metadata)
        {
            components.TryAdd(type, metadata);
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