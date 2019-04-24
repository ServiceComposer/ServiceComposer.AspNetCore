using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public delegate Task EventHandler<TEvent>(string requestId, dynamic pageViewModel, TEvent @event, RouteData routeData, IQueryCollection query);
}