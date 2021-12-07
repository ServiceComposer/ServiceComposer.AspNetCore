using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.ModelBinding
{
    class RawBodyUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: model-binding-raw-body-usage
        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true );
            var body = await reader.ReadToEndAsync();
            var content = JObject.Parse(body);

            //use the content object instance as needed
        }
        // end-snippet
    }

    class RawRouteDataUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: model-binding-raw-route-data-usage
        [HttpPost("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            var routeData = request.HttpContext.GetRouteData();
            var id = int.Parse(routeData.Values["id"].ToString());

            //use the id value as needed

            return Task.CompletedTask;
        }
        // end-snippet
    }
}