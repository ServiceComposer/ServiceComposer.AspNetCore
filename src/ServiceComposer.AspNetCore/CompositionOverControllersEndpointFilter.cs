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

            var viewModel = await CompositionOverControllersComposer.Compose(httpContext, boundHandlers, logger);

            // An action result set by a composition handler replaces the
            // controller one, so that outer filters see the actual outcome.
            if (httpContext.Items.TryGetValue(HttpRequestExtensions.ComposedActionResultKey, out var composedActionResult)
                && composedActionResult is IActionResult handlerActionResult)
            {
                return handlerActionResult;
            }

            CompositionOverControllersComposer.SetViewModel(actionResult, viewModel);
            return actionResult;
        }
    }
}
