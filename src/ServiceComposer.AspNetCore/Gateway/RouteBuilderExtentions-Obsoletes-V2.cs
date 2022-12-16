using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore.Gateway
{
    public static class RouteBuilderExtentions
    {
        [Obsolete(message:"MapComposableGet is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposableGet(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            throw new NotSupportedException();
        }

        [Obsolete(message:"MapComposablePost is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposablePost(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            throw new NotSupportedException();
        }

        [Obsolete(message:"MapComposablePatch is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposablePatch(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            throw new NotSupportedException();
        }

        [Obsolete(message:"MapComposablePut is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposablePut(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            throw new NotSupportedException();
        }

        [Obsolete(message:"MapComposableDelete is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposableDelete(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            throw new NotSupportedException();
        }

        [Obsolete(message:"MapComposableRoute is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposableRoute(this IRouteBuilder routeBuilder,
           string template,
           IDictionary<string, object> constraints,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            throw new NotSupportedException();
        }
    }
}
