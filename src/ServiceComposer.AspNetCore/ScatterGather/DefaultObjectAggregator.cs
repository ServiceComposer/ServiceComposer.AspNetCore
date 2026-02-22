using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

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
