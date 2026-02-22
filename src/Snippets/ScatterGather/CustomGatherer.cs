using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

// begin-snippet: scatter-gather-custom-gatherer
class CustomGatherer : IGatherer
{
    public string Key { get; } = "CustomGatherer";

    public Task<IEnumerable<object>> Gather(HttpContext context)
    {
        var data = (IEnumerable<object>)new[] { new { Value = "ACustomSample" } };
        return Task.FromResult(data);
    }
}
// end-snippet
