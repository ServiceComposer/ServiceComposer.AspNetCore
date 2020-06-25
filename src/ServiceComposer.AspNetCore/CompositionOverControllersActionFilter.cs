#if NETCOREAPP3_1
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            if (endpoint != null)
            {
                var handlerTypes = _compositionOverControllersRoutes.HandlersForRoute(
                    endpoint.RoutePattern.RawText,
                    context.HttpContext.Request.Method);
                
                if (handlerTypes.Any())
                {
                    var (viewModel, statusCode) = await CompositionHandler.HandleComposableRequest(context.HttpContext, handlerTypes);
                    switch (context.Result)
                    {
                        case ViewResult viewResult when viewResult.ViewData.Model == null:
                        {
                            //MVC
                            if (statusCode == StatusCodes.Status200OK)
                            {
                                viewResult.ViewData.Model = viewModel;
                            }

                            break;
                        }
                        case ObjectResult objectResult when objectResult.Value == null:
                        {
                            //WebAPI
                            if (statusCode == StatusCodes.Status200OK)
                            {
                                objectResult.Value = viewModel;
                            }

                            break;
                        }
                    }
                }
            }

            await next();
        }
    }
}
#endif