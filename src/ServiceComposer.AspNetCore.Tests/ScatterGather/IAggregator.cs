using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public interface IAggregator
{
    void Add(IEnumerable<JsonNode> nodes);
    Task<JsonArray> Aggregate();
}