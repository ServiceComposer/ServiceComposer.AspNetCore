using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ServiceComposer.AspNetCore
{
    [Obsolete(message:"IInterceptRoutes is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition.", error:false)]
    public interface IInterceptRoutes
    {
        bool Matches(RouteData routeData, string httpVerb, HttpRequest request);
    }
}
