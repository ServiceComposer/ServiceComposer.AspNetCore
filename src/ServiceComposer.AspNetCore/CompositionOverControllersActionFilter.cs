#if NETCOREAPP3_1
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ServiceComposer.AspNetCore
{
    internal class CompositionOverControllersActionFilter : IAsyncResultFilter
    {
        private readonly CompositionOverControllersRoutes _compositionOverControllersRoutes;

        public CompositionOverControllersActionFilter(CompositionOverControllersRoutes compositionOverControllersRoutes)
        {
            _compositionOverControllersRoutes = compositionOverControllersRoutes;
        }
        
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint() as RouteEndpoint;
            var handlerTypes = _compositionOverControllersRoutes.HandlersForRoute(endpoint.RoutePattern.RawText,
                context.HttpContext.Request.Method);
            if (handlerTypes.Any())
            {
                
            }

            await next();
        }
    }
}
#endif