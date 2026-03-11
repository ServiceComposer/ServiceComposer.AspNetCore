using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.Authentication;

// begin-snippet: multiple-handlers-different-auth-requirements
public class SalesHandler : ICompositionRequestsHandler
{
    [Authorize]
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request) { /* ... */ return Task.CompletedTask; }
}

public class InventoryHandler : ICompositionRequestsHandler
{
    [Authorize(Policy = "WarehouseStaff")]
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request) { /* ... */ return Task.CompletedTask; }
}
// end-snippet

static class AuthSetupSnippets
{
    static void ShowSetup()
    {
        // begin-snippet: auth-middleware-setup
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddViewModelComposition();
        builder.Services.AddAuthentication(); // configure your scheme here
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCompositionHandlers();
        app.Run();
        // end-snippet
    }
}
