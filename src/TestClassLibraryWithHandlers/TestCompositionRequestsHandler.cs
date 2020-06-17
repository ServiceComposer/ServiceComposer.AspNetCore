using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers
{
    public class TestCompositionRequestsHandler : ICompositionRequestsHandler
    {
        [HttpGet("/empty-response/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
}