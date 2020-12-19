using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.Http.Headers;

namespace ServiceComposer.AspNetCore
{
    public class ComposedRequestIdHeader
    {
        public const string Key = "composed-request-id";
    }

    public static class ComposedRequestIdHeaderExtensions
    {
        [Obsolete(message:"AddComposedRequestIdHeader is obsolete, it'll be treated as an error starting v2 and removed in v3.", error:false)]
        public static void AddComposedRequestIdHeader(this HttpRequestHeaders headers, string requestId)
        {
            headers.Add(ComposedRequestIdHeader.Key, requestId);
        }

        //TODO: make internal
        [Obsolete(message:"AddComposedRequestIdHeader is obsolete, it'll be treated as an error starting v2 and removed in v3.", error:false)]
        public static void AddComposedRequestIdHeader(this IHeaderDictionary headers, string requestId)
        {
            headers.Add(ComposedRequestIdHeader.Key, requestId);
        }

        [Obsolete(message:"GetComposedRequestIdHeaderOr is obsolete, it'll be treated as an error starting v2 and removed in v3. Use GetCompositionContext().RequestId.", error:false)]
        public static string GetComposedRequestIdHeaderOr(this IHeaderDictionary headers, Func<string> defaultValue)
        {
            return headers.ContainsKey(ComposedRequestIdHeader.Key)
                ? headers[ComposedRequestIdHeader.Key].Single()
                : defaultValue();
        }

        [Obsolete(message:"GetComposedRequestId is obsolete, it'll be treated as an error starting v2 and removed in v3. Use GetCompositionContext().RequestId.", error:false)]
        public static string GetComposedRequestId(this IHeaderDictionary headers)
        {
            return GetComposedRequestIdHeaderOr(headers, () => null);
        }
    }
}
