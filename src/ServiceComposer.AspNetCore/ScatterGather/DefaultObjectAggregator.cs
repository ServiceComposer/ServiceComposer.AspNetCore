using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

// Used when UseOutputFormatters = true. Keeps items as their original CLR types so that MVC
// output formatters receive the real runtime type for content negotiation (JSON, XML, etc.).
// Handing a pre-serialized JsonArray to formatters would lose type fidelity.
class DefaultObjectAggregator : IAggregator
{
    readonly ConcurrentBag<object> allObjects = new();

    public void Add(IEnumerable<object> nodes)
    {
        foreach (var node in nodes)
        {
            allObjects.Add(node);
        }
    }

    public Task<object> Aggregate()
    {
        return Task.FromResult((object)allObjects.ToArray());
    }
}
