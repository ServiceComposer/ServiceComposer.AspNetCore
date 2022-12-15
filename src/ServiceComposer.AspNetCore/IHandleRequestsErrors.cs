using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    [Obsolete(message:"IHandleRequestsErrors is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition and ICompositionErrorsHandler.", error:true)]
    public interface IHandleRequestsErrors : IInterceptRoutes
    {
        Task OnRequestError(string requestId, Exception ex, dynamic vm, RouteData routeData, HttpRequest request);
    }
}
