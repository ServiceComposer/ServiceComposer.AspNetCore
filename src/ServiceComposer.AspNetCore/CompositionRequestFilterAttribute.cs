using System;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

/// <summary>
/// Intercepts the invocation of the composition handler whose class or method it's applied to.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class CompositionRequestFilterAttribute : Attribute, ICompositionRequestFilter
{
    public abstract ValueTask InvokeAsync(CompositionRequestFilterContext context,
        CompositionRequestFilterDelegate next);
}
