using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelFactory
    {
        object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext);
    }
}