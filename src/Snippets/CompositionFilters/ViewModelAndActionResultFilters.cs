using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.CompositionFilters
{
    public class ProductPriceHandler : ICompositionRequestsHandler
    {
        [ProductPriceFilter, ValidateProductIdFilter]
        [HttpGet("/products/{id}")]
        public Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel();
            vm.Price = 42.0m;

            return Task.CompletedTask;
        }
    }

    // begin-snippet: composition-filter-view-model
    public class ProductPriceFilterAttribute : CompositionRequestFilterAttribute
    {
        public override async ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
        {
            var vm = context.HttpContext.Request.GetComposedResponseModel();

            // Before the handler runs
            vm.Currency = "EUR";

            await next(context);

            // After the handler ran: read and change what it composed
            vm.FormattedPrice = $"{vm.Price:0.00} {vm.Currency}";
        }
    }
    // end-snippet

    // begin-snippet: composition-filter-action-result
    public class ValidateProductIdFilterAttribute : CompositionRequestFilterAttribute
    {
        public override ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
        {
            var request = context.HttpContext.Request;
            if (!int.TryParse(request.RouteValues["id"]?.ToString(), out _))
            {
                var problems = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "Id", ["The supplied id is not a valid product identifier."] }
                });

                // Replaces the composed response, and skips the handler
                request.SetActionResult(new BadRequestObjectResult(problems));
                return ValueTask.CompletedTask;
            }

            return next(context);
        }
    }
    // end-snippet
}
