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
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {

        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Options.OnAuthenticate(Request);
        }
    }
}