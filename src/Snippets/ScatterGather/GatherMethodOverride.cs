using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class GatherMethodOverride
{
    // begin-snippet: scatter-gather-gather-override
    public class CustomHttpGatherer(string key, string destination) : HttpGatherer(key, destination)
    {
        public override Task<IEnumerable<JsonNode>> Gather(HttpContext context)
        {
            return base.Gather(context);
        }
    }
    // end-snippet
}
