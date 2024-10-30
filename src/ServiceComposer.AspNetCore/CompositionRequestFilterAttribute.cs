using System;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class CompositionRequestFilterAttribute : Attribute, ICompositionRequestFilter
{
    public abstract ValueTask<object> InvokeAsync(CompositionRequestFilterContext context,
        CompositionRequestFilterDelegate next);
}