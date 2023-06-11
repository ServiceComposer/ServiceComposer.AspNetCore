using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public interface IAggregator
{
    void Add(HttpResponseMessage response);
    Task<string> Aggregate();
}