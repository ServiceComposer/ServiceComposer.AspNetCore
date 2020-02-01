using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Gateway
{
    public static class Composition
    {
        public static async Task HandleRequest(HttpContext context)
        {
            var requestId = context.Request.Headers.GetComposedRequestIdHeaderOr(() => Guid.NewGuid().ToString());
            var (viewModel, statusCode) = await CompositionHandler.HandleRequest(requestId, context);
            context.Response.Headers.AddComposedRequestIdHeader(requestId);

            if (statusCode == StatusCodes.Status200OK)
            {
                string json = JsonSerializer.Serialize(viewModel, options: GetSerializerOptions(context));
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(json);
            }
            else
            {
                context.Response.StatusCode = statusCode;
            }
        }

        static JsonSerializerOptions GetSerializerOptions(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Accept-Casing", out StringValues casing))
            {
                casing = "casing/camel";
            }

            switch (casing)
            {
                case "casing/pascal":
                    //uses default options
                    return null;

                default: // "casing/camel":
                    return new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
            }
        }
    }
}
