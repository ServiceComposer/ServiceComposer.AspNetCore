using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersEndpointFilter(ILogger<CompositionOverControllersEndpointFilter> logger) : IEndpointFilter
    {
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);
            if (result is not IActionResult actionResult)
            {
                return result;
            }

            var httpContext = context.HttpContext;
            var (composed, viewModel) = await CompositionOverControllersComposer.TryCompose(httpContext, logger);
            if (!composed)
            {
                return result;
            }

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
