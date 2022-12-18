using System;

namespace ServiceComposer.AspNetCore;

partial class ViewModelCompositionOptions
{
    [Obsolete(message:"RegisterRequestsHandler is obsolete, see upgrade guide on how to use the new composition interfaces.", error:true)]
    public void RegisterRequestsHandler<T>()
    {
        throw new NotSupportedException();
    }

    [Obsolete(message:"RegisterCompositionEventsSubscriber is obsolete, see upgrade guide on how to use the new composition interfaces.", error:true)]
    public void RegisterCompositionEventsSubscriber<T>()
    {
        throw new NotSupportedException();
    }
}