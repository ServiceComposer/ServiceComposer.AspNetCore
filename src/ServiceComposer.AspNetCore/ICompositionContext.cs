#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface ICompositionContext
    {
        string RequestId { get; }
        Task RaiseEvent<TEvent>(TEvent @event);
        [Experimental("SC0001")]
        IList<ModelBindingArgument>? GetArguments(Type owningComponentType);
    }
}