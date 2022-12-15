using System;

namespace ServiceComposer.AspNetCore
{
    [Obsolete(message:"IPublishCompositionEvents is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition and ICompositionEventsPublisher.", error:true)]
    public interface IPublishCompositionEvents
    {
        void Subscribe<TEvent>(EventHandler<TEvent> handler);
    }
}
