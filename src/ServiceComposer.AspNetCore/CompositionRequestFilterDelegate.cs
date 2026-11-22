using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

/// <summary>
/// The next step of a composition handler's filter pipeline: the next filter, or the composition handler itself.
/// </summary>
public delegate ValueTask CompositionRequestFilterDelegate(CompositionRequestFilterContext context);
