using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public interface IGatherer
{
    string Key { get; }
    Task<IEnumerable<object>> Gather(HttpContext context);
}