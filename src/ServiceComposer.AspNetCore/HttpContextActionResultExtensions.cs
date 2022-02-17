#if NETCOREAPP3_1 || NET5_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
    static class HttpContextActionResultExtensions
    {
        private static readonly RouteData EmptyRouteData = new RouteData();

        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        /// <summary>
        /// Write a model to the response.
        /// </summary>
        public static Task WriteModelAsync<TModel>(this HttpContext context, TModel model)
        {
            var result = new ObjectResult(model)
            {
                DeclaredType = typeof(TModel)
            };

            return context.ExecuteResultAsync(result);
        }

        /// <summary>
        /// Write any action result to the response.
        /// </summary>
        public static Task ExecuteResultAsync(this HttpContext context, IActionResult result)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var routeData = context.GetRouteData() ?? EmptyRouteData;
            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            return result.ExecuteResultAsync(actionContext);
        }
    }
}
#endif