using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers;

public class TestHandlerUsingEndpointScopedFactory : ICompositionRequestsHandler
{
    [HttpGet("/use-endpoint-scoped-factory/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel<TestModel>();
        vm.Value = int.Parse(request.RouteValues["id"].ToString());
        
        return Task.CompletedTask;
    }
}