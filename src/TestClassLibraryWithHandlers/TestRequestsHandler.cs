using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers
{
    public class TestRequestsHandler : IHandleRequests
    {
        public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
        {
            return Task.CompletedTask;
        }

        public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
        {
            return (string)routeData.Values["controller"] == "matching-handlers";
        }
    }
}