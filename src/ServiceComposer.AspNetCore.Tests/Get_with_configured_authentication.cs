using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class Get_with_configured_authentication
    {
        class RestrictedEmptyResponseHandler : ICompositionRequestsHandler
        {
            [Authorize]
            [HttpGet("/not-authorized-response/{id}")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }
        
        class NotRestrictedEmptyResponseHandler : ICompositionRequestsHandler
        {
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
                        options.RegisterCompositionHandler<RestrictedEmptyResponseHandler>();
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
        
        [Fact]
        public async Task When_not_all_handlers_are_restricted_is_Unauthorized()
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
                        options.RegisterCompositionHandler<RestrictedEmptyResponseHandler>();
                        options.RegisterCompositionHandler<NotRestrictedEmptyResponseHandler>();
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