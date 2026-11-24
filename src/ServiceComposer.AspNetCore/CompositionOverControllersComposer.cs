using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    /// <summary>
    /// Runs the composition handlers matching the route of the current controller action.
    /// </summary>
    static class CompositionOverControllersComposer
    {
        /// <summary>
        /// The composition handlers matching the route of the current controller action, with their bound models.
        /// </summary>
        internal sealed class BoundHandlers(
            (Type ComponentType, IList<object> Metadata)[] handlers,
            IDictionary<Type, IList<ModelBindingArgument>> arguments,
            ModelStateDictionary modelState,
            string rawTemplate)
        {
            public (Type ComponentType, IList<object> Metadata)[] Handlers { get; } = handlers;
            public IDictionary<Type, IList<ModelBindingArgument>> Arguments { get; } = arguments;
            public ModelStateDictionary ModelState { get; } = modelState;
            public string RawTemplate { get; } = rawTemplate;
        }

        /// <summary>
        /// Binds the models of the composition handlers matching the route of the current
        /// controller action, or returns <c>null</c> if no handler matches.
        /// </summary>
        public static async Task<BoundHandlers> TryBind(HttpContext context)
        {
            if (context.GetEndpoint() is not RouteEndpoint endpoint)
            {
                return null;
            }

            var routes = context.RequestServices.GetRequiredService<CompositionOverControllersRoutes>();

            Debug.Assert(endpoint.RoutePattern.RawText != null, "endpoint.RoutePattern.RawText != null");
            var rawTemplate = routes.UseCaseInsensitiveRouteMatching
                ? endpoint.RoutePattern.RawText.ToLowerInvariant()
                : endpoint.RoutePattern.RawText;
            var handlers = routes.HandlersForRoute(rawTemplate, context.Request.Method);

            if (handlers.Length == 0)
            {
                return null;
            }

            // We need the body to be seekable otherwise if more than one
            // composition handler tries to bind a model to the body
            // it'll fail and only the first one succeeds
            context.Request.EnableBuffering();

            var (arguments, modelState) = await ComponentsModelBinder.BindAll(handlers, context);
            return new BoundHandlers(handlers, arguments, modelState, rawTemplate);
        }

        /// <summary>
        /// Runs the bound composition handlers, returning the composed view model.
        /// </summary>
        public static async Task<object> Compose(HttpContext context, BoundHandlers boundHandlers, object controllerModel, ILogger logger)
        {
            var requestId = context.EnsureRequestIdIsSetup();
            var compositionContext = new CompositionContext
            (
                requestId,
                context.Request,
                context.RequestServices.GetRequiredService<CompositionMetadataRegistry>(),
                boundHandlers.Arguments
            );

            var handlerTypes = boundHandlers.Handlers.Select(h => h.ComponentType).ToArray();
            logger.LogDebug("Composing over controller action at {Method} {Template} with {HandlerCount} handler(s).",
                context.Request.Method, boundHandlers.RawTemplate, handlerTypes.Length);
            return await CompositionHandler.HandleComposableRequest(context, compositionContext, handlerTypes, controllerModel);
        }

        /// <summary>
        /// Whether the action result is an error, e.g. <c>NotFound(problemDetails)</c> or <c>Forbid()</c>.
        /// Error results aren't composed.
        /// </summary>
        public static bool IsErrorResult(IActionResult result) => result switch
        {
            IStatusCodeActionResult { StatusCode: >= 400 } => true,
            ChallengeResult or ForbidResult => true,
            _ => false
        };

        /// <summary>
        /// Whether the action result can carry the composed view model, and the model the controller
        /// action set on it, if any. Results that can't carry a model, e.g. redirects, are composed,
        /// and the composed view model is discarded.
        /// </summary>
        public static bool CanCarryViewModel(IActionResult result, out object controllerModel)
        {
            switch (result)
            {
                case ViewResult { ViewData: not null } viewResult:
                    controllerModel = viewResult.ViewData.Model;
                    return true;
                case PartialViewResult { ViewData: not null } partialViewResult:
                    controllerModel = partialViewResult.ViewData.Model;
                    return true;
                case ObjectResult objectResult when objectResult.StatusCode is null or (>= 200 and < 300):
                    controllerModel = objectResult.Value;
                    return true;
                default:
                    controllerModel = null;
                    return false;
            }
        }

        /// <summary>
        /// Sets the composed view model on the action result. When the action set a model,
        /// the handlers composed into it, so it's already the composed view model.
        /// </summary>
        public static void SetViewModel(IActionResult result, object viewModel)
        {
            switch (result)
            {
                case ViewResult { ViewData: not null } viewResult when viewResult.ViewData.Model == null:
                {
                    //MVC
                    viewResult.ViewData.Model = viewModel;

                    break;
                }
                case PartialViewResult { ViewData: not null } partialViewResult when partialViewResult.ViewData.Model == null:
                {
                    //MVC
                    partialViewResult.ViewData.Model = viewModel;

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
