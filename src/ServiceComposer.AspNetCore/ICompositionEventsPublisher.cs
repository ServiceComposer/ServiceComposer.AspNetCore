namespace ServiceComposer.AspNetCore
{
    public interface ICompositionEventsPublisher
    {
        void Subscribe<TEvent>(EventHandler<TEvent> handler);
    }
}