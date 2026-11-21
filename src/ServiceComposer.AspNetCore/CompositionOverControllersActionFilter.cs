using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersActionFilter : IAsyncResultFilter
    {
        readonly ILogger<CompositionOverControllersActionFilter> _logger;

        public CompositionOverControllersActionFilter(ILogger<CompositionOverControllersActionFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Endpoints using WithCompositionOverControllers() compose in the endpoint filter pipeline
            if (!CompositionOverControllersComposer.IsComposed(context.HttpContext))
            {
                var (composed, viewModel) = await CompositionOverControllersComposer.TryCompose(context.HttpContext, _logger);
                if (composed)
                {
                    CompositionOverControllersComposer.SetViewModel(context.Result, viewModel);
                }
            }

            await next();
        }
    }
}
