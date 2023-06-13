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

    public abstract Task<IEnumerable<object>> Gather(HttpContext context);
}