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
        
        [Experimental("SC0001", UrlFormat = "https://github.com/ServiceComposer/ServiceComposer.AspNetCore/blob/master/docs/model-binding.md#named-arguments-experimental-api?id={0}")]
        IList<ModelBindingArgument>? GetArguments(ICompositionRequestsHandler owner);
        
        [Experimental("SC0001", UrlFormat = "https://github.com/ServiceComposer/ServiceComposer.AspNetCore/blob/master/docs/model-binding.md#named-arguments-experimental-api?id={0}")]
        IList<ModelBindingArgument>? GetArguments(ICompositionEventsSubscriber owner);
        
        [Experimental("SC0001", UrlFormat = "https://github.com/ServiceComposer/ServiceComposer.AspNetCore/blob/master/docs/model-binding.md#named-arguments-experimental-api?id={0}")]
        IList<ModelBindingArgument>? GetArguments<T>(ICompositionEventsHandler<T> owner);
    }
}