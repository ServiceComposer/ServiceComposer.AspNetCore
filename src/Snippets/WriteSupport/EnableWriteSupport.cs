using System;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.WriteSupport;

public class DisableWriteSupport
{
    // begin-snippet: disable-write-support
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewModelComposition(options => options.DisableWriteSupport());
    } 
    // end-snippet
}