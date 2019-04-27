using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public interface IHandleRequests : IInterceptRoutes
    {
        Task Handle(string requestId, RouteData routeData, HttpRequest request);
    }
}
