﻿using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface ICompositionContext
    {
        string RequestId { get; }
        Task RaiseEvent(object @event);
    }
}