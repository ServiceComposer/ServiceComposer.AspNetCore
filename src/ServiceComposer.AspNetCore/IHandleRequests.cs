using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    [Obsolete(message:"IHandleRequests is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition and ICompositionRequestsHandler.", error:false)]
    public interface IHandleRequests : IInterceptRoutes
    {
        Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request);
    }
}
