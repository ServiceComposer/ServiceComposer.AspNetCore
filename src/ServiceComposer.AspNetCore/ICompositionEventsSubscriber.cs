namespace ServiceComposer.AspNetCore
{
    public interface ICompositionEventsSubscriber
    {
        void Subscribe(ICompositionEventsPublisher publisher);
    }
}