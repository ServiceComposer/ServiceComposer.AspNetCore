using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Gateway
{
    public static class ApplicationBuilderExtensions
    {
        [Obsolete(message:"CompositionGateway is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static void RunCompositionGateway(this IApplicationBuilder app, Action<IRouteBuilder> routes = null)
        {
            var routeBuilder = new RouteBuilder(app);
            routes?.Invoke(routeBuilder);

            app.UseRouter(routeBuilder.Build());
        }

        [Obsolete(message:"CompositionGateway is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static void RunCompositionGatewayWithDefaultRoutes(this IApplicationBuilder app)
        {
            app.RunCompositionGateway(routes =>
            {
                routes.MapComposableGet( template: "{controller}/{id:int?}");
                routes.MapRoute("{*NotFound}", context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                });
            });
        }
    }
}