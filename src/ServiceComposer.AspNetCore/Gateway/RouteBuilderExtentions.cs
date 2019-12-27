using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Gateway
{
    public static class RouteBuilderExtentions
    {
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
