using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

class DefaultAggregator : IAggregator
{
    readonly ConcurrentBag<JsonNode> allNodes = new();

    public void Add(IEnumerable<JsonNode> nodes)
    {
        foreach (var node in nodes)
        {
            allNodes.Add(node);
        }
    }

    public Task<JsonArray> Aggregate()
    {
        var responsesArray = new JsonArray(allNodes.ToArray());
        return Task.FromResult(responsesArray);
    }
}