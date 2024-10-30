using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore;

public delegate ValueTask<object> CompositionRequestFilterDelegate(CompositionRequestFilterContext context);