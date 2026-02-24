using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers
{
    public class TestRequestsHandler : ICompositionRequestsHandler
    {
        [HttpGet("/matching-handlers")]
        [HttpGet("/matching-handlers/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
}