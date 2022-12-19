using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.UpgradeGuides._1.x_to_2._0;

public class UpgradeGuide
{
    public class RunCompositionGatewayDeprecation
    {
        // begin-snippet: run-composition-gateway-deprecation
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseRouting();
            app.UseEndpoints(builder => builder.MapCompositionHandlers());
        }
        // end-snippet
    }
    
    public class CompositionOverControllers
    {
        // begin-snippet: composition-over-controllers-case-sensitive
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddViewModelComposition(config =>
            {
                config.EnableCompositionOverControllers(useCaseInsensitiveRouteMatching: false);
            });
        }
        // end-snippet
    }
    
    // begin-snippet: composition-handler-api
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet
}