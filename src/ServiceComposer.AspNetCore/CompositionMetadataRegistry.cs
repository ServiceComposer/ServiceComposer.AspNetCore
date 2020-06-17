using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    internal class CompositionMetadataRegistry
    {
        internal HashSet<Type> Components { get; } = new HashSet<Type>();

        public void AddComponent(Type type)
        {
            Components.Add(type);
        }
    }
}