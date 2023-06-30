using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public abstract class Gatherer<T> : IGatherer where T : class
{
    protected Gatherer(string key)
    {
        Key = key;
    }

    public string Key { get; }

    Task<IEnumerable<object>> IGatherer.Gather(HttpContext context)
    {
        return Gather(context).ContinueWith(t => (IEnumerable<object>)t.Result);
    }

    public abstract Task<IEnumerable<T>> Gather(HttpContext context);
}