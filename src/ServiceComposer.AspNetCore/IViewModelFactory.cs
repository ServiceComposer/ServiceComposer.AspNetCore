using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    interface IViewModelFactory
    {
        DynamicViewModel CreateViewModel(HttpContext httpContext, CompositionContext compositionContext);
    }
}