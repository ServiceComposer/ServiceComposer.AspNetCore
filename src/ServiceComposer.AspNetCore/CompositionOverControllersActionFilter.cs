#if NETCOREAPP3_1 || NET5_0
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
        private readonly CompositionOverControllersOptions _compositionOverControllersOptions;

        public CompositionOverControllersActionFilter(CompositionOverControllersRoutes compositionOverControllersRoutes, ViewModelCompositionOptions viewModelCompositionOptions)
        {
            _compositionOverControllersRoutes = compositionOverControllersRoutes;
            _compositionOverControllersOptions = viewModelCompositionOptions.CompositionOverControllersOptions;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint() as RouteEndpoint;
            if (endpoint != null)
            {
                var rawTemplate = _compositionOverControllersOptions.UseCaseInsensitiveRouteMatching
                    ? endpoint.RoutePattern.RawText.ToLowerInvariant()
                    : endpoint.RoutePattern.RawText;
                var handlerTypes = _compositionOverControllersRoutes.HandlersForRoute(rawTemplate, context.HttpContext.Request.Method);

                if (handlerTypes.Any())
                {
                    var viewModel = await CompositionHandler.HandleComposableRequest(context.HttpContext, handlerTypes);
                    switch (context.Result)
                    {
                        case ViewResult viewResult when viewResult.ViewData.Model == null:
                        {
                            //MVC
                            viewResult.ViewData.Model = viewModel;

                            break;
                        }
                        case ObjectResult objectResult when objectResult.Value == null:
                        {
                            //WebAPI
                            objectResult.Value = viewModel;

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