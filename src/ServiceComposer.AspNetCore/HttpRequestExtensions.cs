using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestExtensions
    {
        public static readonly string ComposedResponseModelKey = "composed-response-model";

        public static dynamic GetResponseModel(this HttpRequest request)
        {
            return request.HttpContext.Items[ComposedResponseModelKey];
        }
    }
}
