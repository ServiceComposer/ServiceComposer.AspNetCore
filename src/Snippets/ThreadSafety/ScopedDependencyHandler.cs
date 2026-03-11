using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ThreadSafety;

// Stub representing a non-thread-safe service (e.g. a DbContext)
class SalesDbContext
{
    public Task<decimal> GetProductPriceAsync(string productId)
        => Task.FromResult(100m);
}

// begin-snippet: thread-safety-child-scope
public class SalesHandler : ICompositionRequestsHandler
{
    readonly IServiceProvider _serviceProvider;

    public SalesHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpGet("/product/{id}")]
    public async Task Handle(HttpRequest request)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var vm = request.GetComposedResponseModel();
        vm.ProductPrice = await db.GetProductPriceAsync(
            request.RouteValues["id"].ToString());
    }
}
// end-snippet
