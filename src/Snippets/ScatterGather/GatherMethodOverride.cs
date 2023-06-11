using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class GatherMethodOverride
{
    // begin-snippet: scatter-gather-gather-override
    public class CustomHttpGatherer : HttpGatherer
    {
        public CustomHttpGatherer(string key, string destination) : base(key, destination) { }

        public override Task<IEnumerable<JsonNode>> Gather(HttpContext context)
        {
            // by overriding this method we can implement custom logic
            // to gather the responses from the downstream service.
            return base.Gather(context);
        }
    }
    // end-snippet
}