using System;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public static class HttpContextExtensions
{
    public static string EnsureRequestIdIsSetup(this HttpContext context)
    {
        if(!context.Request.Headers.TryGetValue(ComposedRequestIdHeader.Key, out var requestId))
        {
            requestId = Guid.NewGuid().ToString();
        }

        context.Response.Headers.Append(ComposedRequestIdHeader.Key, requestId);
        return requestId;
    }
}