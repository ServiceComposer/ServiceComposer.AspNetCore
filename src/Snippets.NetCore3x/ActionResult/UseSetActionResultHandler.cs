using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.ActionResult
{
    // begin-snippet: net-core-3x-action-results
    public class UseSetActionResultHandler : ICompositionRequestsHandler
    {
        [HttpGet("/product/{id}")]
        public Task Handle(HttpRequest request)
        {
            var id = request.RouteValues["id"];

            //validate the id format

            var problems = new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                { "Id", new []{ "The supplied id does not respect the identifier format." } }
            });
            var result = new BadRequestObjectResult(problems);

            request.SetActionResult(result);

            return Task.CompletedTask;
        }
    }
    // end-snippet
}