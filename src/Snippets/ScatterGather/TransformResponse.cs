using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class TransformResponseMethodOverride
{
    // begin-snippet: scatter-gather-transform-response
    public class CustomHttpGatherer : HttpGatherer
    {
        public CustomHttpGatherer(string key, string destination) : base(key, destination) { }

        protected override Task<IEnumerable<JsonNode>> TransformResponse(HttpResponseMessage responseMessage)
        {
            // retrieve the response as a string from the HttpResponseMessage
            // and parse it as a JsonNode enumerable.
            return base.TransformResponse(responseMessage);
        }
    }
    // end-snippet
}
