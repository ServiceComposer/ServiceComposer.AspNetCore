using System;

namespace ServiceComposer.AspNetCore
{
    [Obsolete(message:"ISubscribeToCompositionEvents is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition and ICompositionEventsSubscriber.", error:true)]
    public interface ISubscribeToCompositionEvents : IInterceptRoutes
    {
        void Subscribe(IPublishCompositionEvents publisher);
    }
}
