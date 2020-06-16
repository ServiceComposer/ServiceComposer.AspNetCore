#if NETCOREAPP3_1
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore.Tests
{
    class DelegateAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public Func<HttpRequest, Task<AuthenticateResult>> OnAuthenticate { get; set; }
    }
}
#endif