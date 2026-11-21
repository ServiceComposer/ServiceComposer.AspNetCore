using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersOptions
    {
        public bool IsEnabled { get; set; }
        public bool UseCaseInsensitiveRouteMatching { get; set; }

        /// <summary>
        /// When enabled for endpoints using WithCompositionOverControllers() only,
        /// composition runs only over the endpoints that opted in.
        /// </summary>
        public bool OnlyOptedInEndpoints { get; set; }

        public bool CanComposeOver(Endpoint endpoint) =>
            !OnlyOptedInEndpoints || endpoint.Metadata.GetMetadata<CompositionOverControllersMetadata>() != null;
    }

    /// <summary>
    /// Added by WithCompositionOverControllers() to the controller endpoints opting in.
    /// </summary>
    sealed class CompositionOverControllersMetadata(bool useCaseInsensitiveRouteMatching)
    {
        public bool UseCaseInsensitiveRouteMatching { get; } = useCaseInsensitiveRouteMatching;
    }
}
