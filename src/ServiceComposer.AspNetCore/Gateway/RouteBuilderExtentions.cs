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
            return MapComposableRoute(
                routeBuilder: routeBuilder,
                template: template,
                constraints: new RouteValueDictionary(new
                {
                    httpMethod = new HttpMethodRouteConstraint(HttpMethods.Get)
                }),
                defaults: defaults,
                dataTokens: dataTokens
            );
        }

        [Obsolete(message:"MapComposablePost is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposablePost(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            return MapComposableRoute(
                routeBuilder: routeBuilder,
                template: template,
                constraints: new RouteValueDictionary(new
                {
                    httpMethod = new HttpMethodRouteConstraint(HttpMethods.Post)
                }),
                defaults: defaults,
                dataTokens: dataTokens
            );
        }

        [Obsolete(message:"MapComposablePatch is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposablePatch(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            return MapComposableRoute(
                routeBuilder: routeBuilder,
                template: template,
                constraints: new RouteValueDictionary(new
                {
                    httpMethod = new HttpMethodRouteConstraint(HttpMethods.Patch)
                }),
                defaults: defaults,
                dataTokens: dataTokens
             );
        }

        [Obsolete(message:"MapComposablePut is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposablePut(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            return MapComposableRoute(
                routeBuilder: routeBuilder,
                template: template,
                constraints: new RouteValueDictionary(new
                {
                    httpMethod = new HttpMethodRouteConstraint(HttpMethods.Put)
                }),
                defaults: defaults,
                dataTokens: dataTokens
             );
        }

        [Obsolete(message:"MapComposableDelete is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposableDelete(this IRouteBuilder routeBuilder,
           string template,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            return MapComposableRoute(
                routeBuilder: routeBuilder,
                template: template,
                constraints: new RouteValueDictionary(new
                {
                    httpMethod = new HttpMethodRouteConstraint(HttpMethods.Delete)
                }),
                defaults: defaults,
                dataTokens: dataTokens
            );
        }

        [Obsolete(message:"MapComposableRoute is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static IRouteBuilder MapComposableRoute(this IRouteBuilder routeBuilder,
           string template,
           IDictionary<string, object> constraints,
           RouteValueDictionary defaults = null,
           RouteValueDictionary dataTokens = null)
        {
            var route = new Route(
                target: new RouteHandler(ctx => Composition.HandleRequest(ctx)),
                routeTemplate: template,
                defaults: defaults,
                constraints: constraints,
                dataTokens: dataTokens,
                inlineConstraintResolver: routeBuilder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>()
            );

            routeBuilder.Routes.Add(route);

            return routeBuilder;
        }
    }
}
