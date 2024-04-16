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
    
    [Obsolete("EnableWriteSupport is obsolete. Starting v2.1.0, write support is enabled by default. Use DisableWriteSupport to disable it. It'll be considered an error in v3.0.0 and removed in v4.0.0", false)]
    public void EnableWriteSupport()
    {
        IsWriteSupportEnabled = true;
    }
}