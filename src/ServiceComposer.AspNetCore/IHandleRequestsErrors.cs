using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface IHandleRequestsErrors : IInterceptRoutes
    {
        Task OnRequestError(string requestId, RouteData routeData, HttpRequest request, Exception ex);
    }
}
