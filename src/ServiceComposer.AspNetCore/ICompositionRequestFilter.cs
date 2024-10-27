using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

public interface ICompositionRequestFilter
{
    ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next);
}

public interface ICompositionRequestFilter<T> : ICompositionRequestFilter;