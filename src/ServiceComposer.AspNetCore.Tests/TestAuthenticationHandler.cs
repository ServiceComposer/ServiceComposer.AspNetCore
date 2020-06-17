#if NETCOREAPP3_1
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceComposer.AspNetCore.Tests
{
    class TestAuthenticationHandler : AuthenticationHandler<DelegateAuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(
            IOptionsMonitor<DelegateAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {

        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Options.OnAuthenticate(Request);
        }
    }
}
#endif