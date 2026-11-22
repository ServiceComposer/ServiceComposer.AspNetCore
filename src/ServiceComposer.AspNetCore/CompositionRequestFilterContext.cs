using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public sealed class CompositionRequestFilterContext
{
    internal CompositionRequestFilterContext(HttpContext httpContext, ICompositionRequestsHandler handler)
    {
        HttpContext = httpContext;
        Handler = handler;
    }

    public HttpContext HttpContext { get; }

    /// <summary>
    /// The composition handler the filter is invoked for.
    /// </summary>
    public ICompositionRequestsHandler Handler { get; }
}
