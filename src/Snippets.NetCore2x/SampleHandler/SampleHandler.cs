using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore2x.SampleHandler
{
    // begin-snippet: net-core-2x-sample-handler-with-custom-status-code
    public class SampleHandlerWithCustomStatusCode : IHandleRequests
    {
        public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
        {
            return true;
        }

        public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
        {
            var response = request.HttpContext.Response;
            response.StatusCode = (int)HttpStatusCode.Forbidden;

            return Task.CompletedTask;
        }
    }
    // end-snippet
}