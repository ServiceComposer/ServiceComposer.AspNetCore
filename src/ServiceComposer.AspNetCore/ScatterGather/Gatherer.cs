using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public abstract class Gatherer
{
    protected Gatherer(string key)
    {
        Key = key;
    }

    public string Key { get; }

    // TODO: how to use generics to remove the dependency on JSON?
    public abstract Task<IEnumerable<JsonNode>> Gather(HttpContext context);
}