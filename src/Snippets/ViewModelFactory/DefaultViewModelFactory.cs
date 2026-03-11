using System.Dynamic;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.ViewModelFactory;

// begin-snippet: global-view-model-factory
public class DefaultViewModelFactory : IViewModelFactory
{
    public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
    {
        // Return a shared base type, or inspect the route to determine the right type
        return new ExpandoObject();
    }
}
// end-snippet
