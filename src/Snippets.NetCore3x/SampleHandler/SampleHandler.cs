using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.SampleHandler
{
    // begin-snippet: net-core-3x-sample-handler
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet
}