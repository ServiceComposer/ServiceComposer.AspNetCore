using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

class DefaultAggregator : IAggregator
{
    readonly ConcurrentBag<JsonNode> allNodes = new();

    public void Add(IEnumerable<object> nodes)
    {
        foreach (var node in nodes)
        {
            allNodes.Add((JsonNode)node);
        }
    }

    public Task<object> Aggregate()
    {
        var responsesArray = new JsonArray(allNodes.ToArray());
        return Task.FromResult((object)responsesArray);
    }
}