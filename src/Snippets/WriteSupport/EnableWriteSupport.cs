using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.WriteSupport;

public class EnableWriteSupport
{
    // begin-snippet: enable-write-support
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddViewModelComposition(options => options.EnableWriteSupport());
    } 
    // end-snippet
}