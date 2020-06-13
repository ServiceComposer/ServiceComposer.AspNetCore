namespace ServiceComposer.AspNetCore
{
    public interface ICompositionEventsSubscriber
    {
        void Subscribe(IPublishCompositionEvents publisher);
    }
}