using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.ModelBinding
{
    class RawBodyUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: net-core-3x-model-binding-raw-body-usage
        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true );
            var body = await reader.ReadToEndAsync();
            var content = JObject.Parse(body);

            var vm = request.GetComposedResponseModel();
            vm.AString = content?.SelectToken("AString")?.Value<string>();
        }
        // end-snippet
    }
}