using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceComposer.AspNetCore.Tests
{
    class TestAuthenticationHandler : AuthenticationHandler<DelegateAuthenticationSchemeOptions>
    {
#if NET8_0
        public TestAuthenticationHandler(
            IOptionsMonitor<DelegateAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {

        }
#else
        public TestAuthenticationHandler(
            IOptionsMonitor<DelegateAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {

        }
#endif

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Options.OnAuthenticate(Request);
        }
    }
}