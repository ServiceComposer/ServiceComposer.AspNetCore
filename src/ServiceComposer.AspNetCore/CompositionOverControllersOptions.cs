using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersOptions
    {
        public bool IsEnabled { get; init; }
        public bool UseCaseInsensitiveRouteMatching { get; init; }

        /// <summary>
        /// Composition runs only over the endpoints that opted in with WithCompositionOverControllers().
        /// </summary>
        public static bool CanComposeOver(Endpoint endpoint) =>
            endpoint.Metadata.GetMetadata<CompositionOverControllersMetadata>() != null;
    }

    /// <summary>
    /// Added by WithCompositionOverControllers() to the controller endpoints opting in.
    /// </summary>
    sealed class CompositionOverControllersMetadata(bool useCaseInsensitiveRouteMatching)
    {
        public bool UseCaseInsensitiveRouteMatching { get; } = useCaseInsensitiveRouteMatching;
    }
}
