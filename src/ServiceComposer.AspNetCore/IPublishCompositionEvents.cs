namespace ServiceComposer.AspNetCore
{
    public interface IPublishCompositionEvents
    {
        void Subscribe<TEvent>(EventHandler<TEvent> handler);
    }
}
