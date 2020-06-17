namespace ServiceComposer.AspNetCore
{
    public interface ICompositionEventsPublisher
    {
        void Subscribe<TEvent>(CompositionEventHandler<TEvent> handler);
    }
}