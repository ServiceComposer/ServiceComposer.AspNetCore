using System;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.WriteSupport;

public class EnableWriteSupport
{
    [Obsolete("This snippet is used only by an upgrade guide. The Obsolete is needed to prevent the EnableWriteSupport usage here to cause the snippet compilation to fail.")]
    // begin-snippet: enable-write-support
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewModelComposition(options => options.EnableWriteSupport());
    } 
    // end-snippet
}

public class DisableWriteSupport
{
    // begin-snippet: disable-write-support
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewModelComposition(options => options.DisableWriteSupport());
    } 
    // end-snippet
}