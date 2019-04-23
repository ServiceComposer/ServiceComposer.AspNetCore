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
                target: new RouteHandler(ctx => HandleRequest(ctx)),
                routeTemplate: template,
                defaults: defaults,
                constraints: constraints,
                dataTokens: dataTokens,
                inlineConstraintResolver: routeBuilder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>()
            );

            routeBuilder.Routes.Add(route);

            return routeBuilder;
        }

        static async Task HandleRequest(HttpContext context)
        {
            var requestId = context.Request.Headers.GetComposedRequestIdHeaderOr(() => Guid.NewGuid().ToString());
            var (viewModel, statusCode) = await CompositionHandler.HandleRequest(requestId, context);
            context.Response.Headers.AddComposedRequestIdHeader(requestId);

            if (statusCode == StatusCodes.Status200OK)
            {
                string json = JsonConvert.SerializeObject(viewModel, GetSettings(context));
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(json);
            }
            else
            {
                context.Response.StatusCode = statusCode;
            }
        }

        static JsonSerializerSettings GetSettings(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Accept-Casing", out StringValues casing))
            {
                casing = "casing/camel";
            }

            switch (casing)
            {
                case "casing/pascal":
                    return new JsonSerializerSettings();

                default: // "casing/camel":
                    return new JsonSerializerSettings()
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
            }
        }
    }
}
