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

        public override Task<IEnumerable<object>> Gather(HttpContext context)
        {

            return base.Gather(context);
        }
    }
    // end-snippet
}