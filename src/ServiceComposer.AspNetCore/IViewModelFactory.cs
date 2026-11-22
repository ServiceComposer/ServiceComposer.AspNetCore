#nullable enable
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelFactory
    {
        /// <summary>
        /// Creates the view model of a composition request. Returning <c>null</c> falls through
        /// to the next factory: from the endpoint scoped factory to the global one, and from the
        /// global one to the default dynamic view model.
        /// </summary>
        object? CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext);
    }
}