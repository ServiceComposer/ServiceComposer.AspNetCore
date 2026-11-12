namespace ServiceComposer.AspNetCore
{
    /// <summary>
    /// Registers the composition components and the options customizations
    /// defined in an assembly, without relying on assembly scanning.
    /// Register a provider using
    /// <see cref="ViewModelCompositionOptions.RegisterComponentsFrom{TProvider}"/>.
    /// </summary>
    public interface ICompositionComponentsProvider
    {
        /// <summary>
        /// Applies the options customizations. Invoked for all providers
        /// before any provider registers components.
        /// </summary>
        void Customize(ViewModelCompositionOptions options);

        /// <summary>
        /// Registers the composition components, such as composition handlers,
        /// events subscribers and handlers, view model factories,
        /// preview handlers, and composition request filters.
        /// </summary>
        void RegisterComponents(ViewModelCompositionOptions options);
    }
}
