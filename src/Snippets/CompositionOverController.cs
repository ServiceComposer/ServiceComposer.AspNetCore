using Microsoft.AspNetCore.Builder;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets;

static class CompositionOverControllersSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: enable-composition-over-controllers
        builder.Services.AddViewModelComposition();
        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapControllers()
            .WithCompositionOverControllers();
        app.MapCompositionHandlers();
        // end-snippet
    }

    static void ShowEndpointFilters(WebApplication app)
    {
        // begin-snippet: composition-over-controllers-endpoint-filters
        app.MapControllers()
            // Wraps both the controller action and the composition handlers
            .AddEndpointFilter(async (context, next) =>
            {
                var result = await next(context);
                // result is the action result, including the one
                // set by composition handlers with SetActionResult
                return result;
            })
            // Must be the last endpoint filter added
            .WithCompositionOverControllers();
        // end-snippet
    }
}

// begin-snippet: composition-over-controllers-controller-model
[Route("/products")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        dynamic product = new ExpandoObject();
        product.Name = "Product name";

        // Composition handlers compose into the returned model
        return Ok(product);
    }
}

public class ProductPriceHandler : ICompositionRequestsHandler
{
    [HttpGet("/products/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.Price = 42m;

        return Task.CompletedTask;
    }
}
// end-snippet
