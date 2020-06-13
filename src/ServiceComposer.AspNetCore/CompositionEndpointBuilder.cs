using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointBuilder : EndpointBuilder
    {
        private readonly Type[] _compositionHandlers;
        public RoutePattern RoutePattern { get; set; }

        public int Order { get; set; }

        public CompositionEndpointBuilder(RoutePattern routePattern, IEnumerable<Type> compositionHandlers, int order)
        {
            _compositionHandlers = compositionHandlers.ToArray();
            RoutePattern = routePattern;
            Order = order;
            RequestDelegate = async context =>
            {
                var request = context.Request;
                var routeData = context.GetRouteData();

                var requestId = request.Headers.GetComposedRequestIdHeaderOr(() => Guid.NewGuid().ToString());
                context.Response.Headers.AddComposedRequestIdHeader(requestId);

                var viewModel = new DynamicViewModel(requestId, routeData, request);
                request.SetModel(viewModel);

                var handlers = _compositionHandlers.Select(type => context.RequestServices.GetRequiredService(type)).ToArray();

                foreach (var subscriber in handlers.OfType<ICompositionEventsSubscriber>())
                {
                    subscriber.Subscribe(viewModel);
                }

                var pending = new List<Task>();

                foreach (var handler in handlers.OfType<ICompositionRequestsHandler>())
                {
                    pending.Add(handler.Handle(request));
                }

                if (pending.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
                else
                {
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        var errorHandlers = handlers.OfType<ICompositionErrorsHandler>();
                        foreach (var handler in errorHandlers)
                        {
                            await handler.OnRequestError(request, ex);
                        }

                        throw;
                    }
                }

                context.Response.StatusCode = StatusCodes.Status200OK;
                var json = JsonConvert.SerializeObject(viewModel, GetSettings(context));
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(json);
            };
        }

        JsonSerializerSettings GetSettings(HttpContext context)
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

        public override Endpoint Build()
        {
            var routeEndpoint = new RouteEndpoint(
                RequestDelegate,
                RoutePattern,
                Order,
                new EndpointMetadataCollection(Metadata),
                DisplayName);

            return routeEndpoint;
        }
    }
}