using System;

namespace Snippets.NetCore2x
{
    //begin-snippet: NetCore2x-sample-startup
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.RunCompositionGateway( routeBuilder=>
            {
                routeBuilder.MapComposableGet("{controller}/{id:int}");
            } );
        }
    }
    //end-snippet
}