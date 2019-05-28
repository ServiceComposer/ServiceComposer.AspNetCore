using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface IDynamicViewModel
    {
        Task RaiseEvent(object @event);
        void Merge(IDictionary<string, object> source);
    }
}