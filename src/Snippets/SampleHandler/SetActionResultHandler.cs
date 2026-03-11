using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.SampleHandler;

// begin-snippet: set-action-result-preferred
public class SampleHandler : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        if (!IsValid(request))
        {
            request.SetActionResult(new BadRequestResult());
            return Task.CompletedTask;
        }

        var vm = request.GetComposedResponseModel();
        vm.Data = "...";
        return Task.CompletedTask;
    }

    bool IsValid(HttpRequest request) => request.RouteValues["id"] != null;
}
// end-snippet
