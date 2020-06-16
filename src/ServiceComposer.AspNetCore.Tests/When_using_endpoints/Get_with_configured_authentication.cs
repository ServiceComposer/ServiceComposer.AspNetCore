#if NETCOREAPP3_1

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests.When_using_endpoints
{
    public class Get_with_configured_authentication
    {
        class EmptyResponseHandler : ICompositionRequestsHandler
        {
            [Authorize]
            [HttpGet("/not-authorized-response/{id}")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Is_Unauthorized()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_configured_authentication>
            (
                configureServices: services =>
                {
                    services.AddAuthorization();
                    services.AddAuthentication("BasicAuthentication")
                        .AddScheme<DelegateAuthenticationSchemeOptions, TestAuthenticationHandler>("BasicAuthentication", options =>
                        {
                            options.OnAuthenticate = request => Task.FromResult( AuthenticateResult.Fail("Invalid username or password"));
                        });

                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<EmptyResponseHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseAuthorization();
                    app.UseAuthentication();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/not-authorized-response/1");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}

#endif