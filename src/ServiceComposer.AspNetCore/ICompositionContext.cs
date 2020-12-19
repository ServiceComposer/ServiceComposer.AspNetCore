using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface ICompositionContext
    {
        Task RaiseEvent(object @event);
    }
}