using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

public interface IAggregator
{
    void Add(IEnumerable<object> nodes);
    Task<object> Aggregate();
}
