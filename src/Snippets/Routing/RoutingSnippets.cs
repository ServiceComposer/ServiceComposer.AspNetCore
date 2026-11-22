using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.Routing;

// begin-snippet: routing-attribute
public class ProductDetailsHandler : ICompositionRequestsHandler
{
    [HttpGet("/product/{id:int}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.ProductId = request.RouteValues["id"];
        return Task.CompletedTask;
    }
}
// end-snippet

// begin-snippet: routing-multiple-attributes
public class ProductPriceHandler : ICompositionRequestsHandler
{
    [HttpGet("/product/{id:int}")]
    [HttpGet("/checkout/product/{id:int}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.ProductPrice = 100;
        return Task.CompletedTask;
    }
}
// end-snippet

static class CaseSensitiveRouteMatchingSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddViewModelComposition();
        builder.Services.AddControllers();
        var app = builder.Build();

        // begin-snippet: routing-case-sensitive-composition-over-controllers
        app.MapControllers()
            .WithCompositionOverControllers(useCaseInsensitiveRouteMatching: false);
        app.MapCompositionHandlers();
        // end-snippet
    }
}

// begin-snippet: routing-class-prefix
[Route("/api/products")]
public class ProductsHandler : ICompositionRequestsHandler
{
    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    // GET /api/products
    [HttpGet]
    // GET /catalog/{id}: absolute, not prefixed
    [HttpGet("/catalog/{id:int}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
// end-snippet
