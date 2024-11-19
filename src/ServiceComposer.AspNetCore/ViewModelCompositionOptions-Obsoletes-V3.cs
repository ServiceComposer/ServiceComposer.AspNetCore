using System;

namespace ServiceComposer.AspNetCore;

partial class ViewModelCompositionOptions
{
    [Obsolete("EnableWriteSupport is obsolete. Starting v2.1.0, write support is enabled by default. Use DisableWriteSupport to disable it. It'll be considered an error in v3.0.0 and removed in v4.0.0", true)]
    public void EnableWriteSupport()
    {
        throw new NotSupportedException();
    }
}