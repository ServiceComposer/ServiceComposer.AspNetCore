using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestExtensions
    {
        public static T GetComposedResponseModel<T>(this HttpRequest request)
        {
            return default;
        }
    }
}