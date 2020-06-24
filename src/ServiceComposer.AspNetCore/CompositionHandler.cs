using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ServiceComposer.AspNetCore
{
    public class CompositionHandler
    {
        public static async Task<(dynamic ViewModel, int StatusCode)> HandleRequest(string requestId,
            HttpContext context)
        {
            var routeData = context.GetRouteData();
            var request = context.Request;
            var viewModel = new DynamicViewModel(requestId, routeData, context.Request);

            try
            {
                var interceptors = context.RequestServices.GetServices<IInterceptRoutes>()
                    .Where(a => a.Matches(routeData, request.Method, request))
                    .ToArray();

                foreach (var subscriber in interceptors.OfType<ISubscribeToCompositionEvents>())
                {
                    subscriber.Subscribe(viewModel);
                }

                var pending = new List<Task>();

                foreach (var handler in interceptors.OfType<IHandleRequests>())
                {
                    pending.Add
                    (
                        handler.Handle(requestId, viewModel, routeData, request)
                    );
                }

                if (pending.Count == 0)
                {
                    return (null, StatusCodes.Status404NotFound);
                }
                else
                {
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        var errorHandlers = interceptors.OfType<IHandleRequestsErrors>();
                        if (errorHandlers.Any())
                        {
                            foreach (var handler in errorHandlers)
                            {
                                await handler.OnRequestError(requestId, ex, viewModel, routeData, request);
                            }
                        }

                        throw;
                    }
                }

                return (viewModel, StatusCodes.Status200OK);
            }
            finally
            {
                viewModel.CleanupSubscribers();
            }
        }
        
#if NETCOREAPP3_1
        internal static async Task ComposeResponse(HttpContext context, Type[] handlerTypes)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var routeData = context.GetRouteData();

            var requestId = request.Headers.GetComposedRequestIdHeaderOr(() => Guid.NewGuid().ToString());
            context.Response.Headers.AddComposedRequestIdHeader(requestId);

            var viewModel = new DynamicViewModel(requestId, routeData, request);
            request.SetModel(viewModel);

            var handlers = handlerTypes.Select(type => context.RequestServices.GetRequiredService(type)).ToArray();

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
#endif
    }
}