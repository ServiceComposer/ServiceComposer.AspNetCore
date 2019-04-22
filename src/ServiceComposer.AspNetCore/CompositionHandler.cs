using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    public class CompositionHandler
    {
        static Dictionary<string, IInterceptRoutes[]> cache = new Dictionary<string, IInterceptRoutes[]>();

        public static async Task<(dynamic ViewModel, int StatusCode)> HandleRequest(string requestId, HttpContext context)
        {
            var routeData = context.GetRouteData();
            var request = context.Request;
            var viewModel = new DynamicViewModel(requestId, routeData, context.Request.Query);

            try
            {
                if (!cache.TryGetValue(request.Path, out var interceptors))
                {
                    interceptors = context.RequestServices.GetServices<IInterceptRoutes>()
                        .Where(a => a.Matches(routeData, request.Method, request))
                        .ToArray();

                    cache.Add(request.Path, interceptors);
                }

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
    }
}