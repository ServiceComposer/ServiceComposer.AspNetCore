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
        public static Task HandleRequest(HttpContext context)
        {
            throw new NotSupportedException();
        }
    }
}
