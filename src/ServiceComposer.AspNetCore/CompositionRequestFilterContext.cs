using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public sealed class CompositionRequestFilterContext
{
    internal CompositionRequestFilterContext(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }

    public HttpContext HttpContext { get; }
}