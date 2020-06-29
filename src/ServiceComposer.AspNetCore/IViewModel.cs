using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModel : ICompositionEventsPublisher
    {
        void CleanupSubscribers();
        Task RaiseEvent(object @event);
    }
}