namespace ServiceComposer.AspNetCore
{
    public interface ISubscribeToCompositionEvents : IInterceptRoutes
    {
        void Subscribe(IPublishCompositionEvents publisher);
    }
}
