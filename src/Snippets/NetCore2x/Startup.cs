using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;
using ServiceComposer.AspNetCore.Gateway;

namespace Snippets.NetCore2x
{
    // begin-snippet: net-core-2x-sample-startup
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.RunCompositionGateway(routeBuilder =>
            {
                routeBuilder.MapComposableGet("{controller}/{id:int}");
            });
        }
    }

    // end-snippet
}