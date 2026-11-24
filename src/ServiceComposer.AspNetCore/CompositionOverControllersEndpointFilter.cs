using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersEndpointFilter(ILogger<CompositionOverControllersEndpointFilter> logger) : IEndpointFilter
    {
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var httpContext = context.HttpContext;
            var routes = httpContext.RequestServices.GetRequiredService<CompositionOverControllersRoutes>();
            if (!routes.IsInitialized)
            {
                throw new InvalidOperationException(
                    $"Composition over controllers requires calling {nameof(EndpointsExtensions.MapCompositionHandlers)}() " +
                    $"after mapping the controllers.");
            }

            // The handlers' models are bound before the action, so that binding errors short-circuit it
            var boundHandlers = await CompositionOverControllersComposer.TryBind(httpContext);
            if (boundHandlers is { ModelState.ErrorCount: > 0 } && ModelBindingProblem.FailOnBindingErrors(httpContext))
            {
                return new ModelBindingProblemResult(boundHandlers.ModelState);
            }

            var result = await next(context);
            if (boundHandlers == null || result is not IActionResult actionResult)
            {
                return result;
            }

            if (CompositionOverControllersComposer.IsErrorResult(actionResult))
            {
                logger.LogDebug("Skipping composition over the controller at {Method} {Template}: its {ActionResultType} is an error.",
                    httpContext.Request.Method, boundHandlers.RawTemplate, actionResult.GetType().Name);
                return result;
            }

            // Results that can't carry a model, e.g. redirects, are still composed, so that
            // handlers with side effects, e.g. POST handlers before a redirect, run.
            // The composed view model is discarded.
            var canCarryViewModel = CompositionOverControllersComposer.CanCarryViewModel(actionResult, out var controllerModel);

            object viewModel;
            try
            {
                // The model the controller action returned, if any, is the view model the handlers compose into
                viewModel = await CompositionOverControllersComposer.Compose(httpContext, boundHandlers, controllerModel, logger);
            }
            catch (ModelBindingBadRequestException ex)
            {
                return new ModelBindingProblemResult(ex.ModelState);
            }

            // An action result set by a composition handler replaces the
            // controller one, so that outer filters see the actual outcome.
            if (httpContext.Items.TryGetValue(HttpRequestExtensions.ComposedActionResultKey, out var composedActionResult)
                && composedActionResult is IActionResult handlerActionResult)
            {
                return handlerActionResult;
            }

            if (canCarryViewModel)
            {
                CompositionOverControllersComposer.SetViewModel(actionResult, viewModel);
            }
            else
            {
                logger.LogDebug("The controller action at {Method} {Template} returned a {ActionResultType}, which can't carry a view model: the composed view model is discarded.",
                    httpContext.Request.Method, boundHandlers.RawTemplate, actionResult.GetType().Name);
            }

            return actionResult;
        }
    }
}
