using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

/// <summary>
/// Intercepts the invocation of a composition handler. Filters run right before the handler
/// they're configured for: not calling <c>next</c> skips that handler only.
/// </summary>
/// <remarks>
/// The handlers of a route run concurrently, so the filters of different handlers do too.
/// </remarks>
public interface ICompositionRequestFilter
{
    ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next);
}

/// <summary>
/// Intercepts the invocation of the <typeparamref name="T"/> composition handler.
/// </summary>
public interface ICompositionRequestFilter<T> : ICompositionRequestFilter;
