using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.WriteSupport;

public class EnableWriteSupport
{
#pragma warning disable CS0618 // Type or member is obsolete
    // begin-snippet: enable-write-support
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewModelComposition(options => options.EnableWriteSupport());
    } 
    // end-snippet
#pragma warning restore CS0618 // Type or member is obsolete
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