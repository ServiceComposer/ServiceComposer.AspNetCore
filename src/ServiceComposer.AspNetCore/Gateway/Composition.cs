using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Gateway
{
    public static class Composition
    {
        [Obsolete(message:"HandleRequest is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, MapCompositionHandlers, and MVC Endpoints.", error:true)]
        public static async Task HandleRequest(HttpContext context)
        {
            var requestId = context.Request.Headers.GetComposedRequestIdHeaderOr(() =>
            {
                var id = Guid.NewGuid().ToString();
                context.Request.Headers.AddComposedRequestIdHeader(id);
                return id;
            });
            var (viewModel, statusCode) = await CompositionHandler.HandleRequest(requestId, context);
            context.Response.Headers.AddComposedRequestIdHeader(requestId);

            //to avoid a breaking change we cannot change the tuple returned by CompositionHandler.HandleRequest
            //so the only option here is to check if the viewModel is null. View model is null only when there are
            //no handlers registered for the route, so it's for sure an HTTP404
            if (viewModel != null)
            {
                string json = JsonConvert.SerializeObject(viewModel, GetSettings(context));
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(json);
            }
            else
            {
                await context.Response.WriteAsync(string.Empty);
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
