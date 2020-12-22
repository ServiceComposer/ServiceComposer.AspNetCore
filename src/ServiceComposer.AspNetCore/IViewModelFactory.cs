#if NETCOREAPP3_1 || NET5_0
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public interface IViewModelFactory
    {
        object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext);
    }
}
#endif