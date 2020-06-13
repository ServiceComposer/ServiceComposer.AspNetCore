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
        public static void AddComposedRequestIdHeader(this HttpRequestHeaders headers, string requestId)
        {
            headers.Add(ComposedRequestIdHeader.Key, requestId);
        }

        public static void AddComposedRequestIdHeader(this IHeaderDictionary headers, string requestId)
        {
            headers.Add(ComposedRequestIdHeader.Key, requestId);
        }

        public static string GetComposedRequestIdHeaderOr(this IHeaderDictionary headers, Func<string> defaultValue)
        {
            return headers.ContainsKey(ComposedRequestIdHeader.Key)
                ? headers[ComposedRequestIdHeader.Key].Single()
                : defaultValue();
        }

        public static string GetComposedRequestId(this IHeaderDictionary headers)
        {
            return GetComposedRequestIdHeaderOr(headers, () => null);
        }
    }
}
