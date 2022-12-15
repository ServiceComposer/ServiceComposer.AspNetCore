using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers
{
    public class TestRequestsHandler : ICompositionRequestsHandler
    {
        [HttpGet("/matching-handlers")]
        public Task Handle(HttpRequest request)
        {
            throw new NotImplementedException();
        }
    }
}