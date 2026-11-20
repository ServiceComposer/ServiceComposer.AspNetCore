using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    /// <summary>
    /// Runs the composition handlers matching the route of the current controller
    /// action. Shared by the MVC result filter and the endpoint filter.
    /// </summary>
    static class CompositionOverControllersComposer
    {
        static readonly object ComposedKey = new();

        /// <summary>
        /// Whether the composition already ran for the current request,
        /// e.g. in the endpoint filter, so that the result filter can skip it.
        /// </summary>
        public static bool IsComposed(HttpContext context) => context.Items.ContainsKey(ComposedKey);

        public static async Task<(bool Composed, object ViewModel)> TryCompose(HttpContext context, ILogger logger)
        {
            if (context.GetEndpoint() is not RouteEndpoint endpoint)
            {
                return (false, null);
            }

            var options = context.RequestServices.GetRequiredService<ViewModelCompositionOptions>().CompositionOverControllersOptions;
            var routes = context.RequestServices.GetRequiredService<CompositionOverControllersRoutes>();

            Debug.Assert(endpoint.RoutePattern.RawText != null, "endpoint.RoutePattern.RawText != null");
            var rawTemplate = options.UseCaseInsensitiveRouteMatching
                ? endpoint.RoutePattern.RawText.ToLowerInvariant()
                : endpoint.RoutePattern.RawText;
            var handlers = routes.HandlersForRoute(rawTemplate, context.Request.Method);

            if (!handlers.Any())
            {
                return (false, null);
            }

            context.Items[ComposedKey] = true;

            // We need the body to be seekable otherwise if more than one
            // composition handler tries to bind a model to the body
            // it'll fail and only the first one succeeds
            context.Request.EnableBuffering();

            var argumentsByComponent = await ComponentsModelBinder.BindAll(handlers, context);

            var requestId = context.EnsureRequestIdIsSetup();
            var compositionContext = new CompositionContext
            (
                requestId,
                context.Request,
                context.RequestServices.GetRequiredService<CompositionMetadataRegistry>(),
                argumentsByComponent
            );

            var handlerTypes = handlers.Select(h => h.ComponentType).ToArray();
            logger.LogDebug("Composing over controller action at {Method} {Template} with {HandlerCount} handler(s).",
                context.Request.Method, rawTemplate, handlerTypes.Length);
            var viewModel = await CompositionHandler.HandleComposableRequest(context, compositionContext, handlerTypes);

            return (true, viewModel);
        }

        /// <summary>
        /// Sets the composed view model on the action result, unless the action already set one.
        /// </summary>
        public static void SetViewModel(IActionResult result, object viewModel)
        {
            switch (result)
            {
                case ViewResult viewResult when viewResult.ViewData.Model == null:
                {
                    //MVC
                    viewResult.ViewData.Model = viewModel;

                    break;
                }
                case ObjectResult { Value: null } objectResult:
                {
                    //WebAPI
                    objectResult.Value = viewModel;

                    break;
                }
            }
        }
    }
}
