using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http.Headers;

namespace ServiceComposer.AspNetCore;

public static class ComposedRequestIdHeaderExtensions
{
    [Obsolete(message:"AddComposedRequestIdHeader is obsolete, it'll be treated as an error starting v2 and removed in v3.", error:true)]
    public static void AddComposedRequestIdHeader(this HttpRequestHeaders headers, string requestId)
    {
        throw new NotImplementedException();
    }

    [Obsolete(message:"AddComposedRequestIdHeader is obsolete, it'll be treated as an error starting v2 and removed in v3.", error:true)]
    public static void AddComposedRequestIdHeader(this IHeaderDictionary headers, string requestId)
    {
        throw new NotImplementedException();
    }

    [Obsolete(message:"GetComposedRequestIdHeaderOr is obsolete, it'll be treated as an error starting v2 and removed in v3. Use GetCompositionContext().RequestId.", error:true)]
    public static string GetComposedRequestIdHeaderOr(this IHeaderDictionary headers, Func<string> defaultValue)
    {
        throw new NotImplementedException();
    }

    [Obsolete(message:"GetComposedRequestId is obsolete, it'll be treated as an error starting v2 and removed in v3. Use GetCompositionContext().RequestId.", error:true)]
    public static string GetComposedRequestId(this IHeaderDictionary headers)
    {
        throw new NotImplementedException();
    }
}