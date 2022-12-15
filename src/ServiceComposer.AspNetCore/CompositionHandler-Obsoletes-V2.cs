using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

static partial class CompositionHandler
{
    [Obsolete(message: "HandleRequest is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
    public static Task<(dynamic ViewModel, int StatusCode)> HandleRequest(string requestId,
        HttpContext context)
    {
        throw new NotSupportedException();
    }
}