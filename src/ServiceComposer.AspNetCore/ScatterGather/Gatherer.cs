using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public abstract class Gatherer<T>(string key) : IGatherer
    where T : class
{
    static string ValidateKey(string value, [CallerArgumentExpression(nameof(value))] string name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value;
    }
    
    public string Key { get; } = ValidateKey(key);

    async Task<IEnumerable<object>> IGatherer.Gather(HttpContext context)
    {
        return await Gather(context);
    }

    public abstract Task<IEnumerable<T>> Gather(HttpContext context);
}
