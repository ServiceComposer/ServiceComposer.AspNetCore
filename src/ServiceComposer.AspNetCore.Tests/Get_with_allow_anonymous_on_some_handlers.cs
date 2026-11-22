using System;
using System.Net;
using System.Net.Http;
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
    public class Get_with_allow_anonymous_on_some_handlers
    {
        class AuthorizeHandler : ICompositionRequestsHandler
        {
            [Authorize]
            [HttpGet("/restricted/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class AllowAnonymousOnRestrictedRouteHandler : ICompositionRequestsHandler
        {
            [AllowAnonymous]
            [HttpGet("/restricted/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class NotRestrictedHandler : ICompositionRequestsHandler
        {
            [HttpGet("/not-restricted/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class AllowAnonymousOnNotRestrictedRouteHandler : ICompositionRequestsHandler
        {
            [AllowAnonymous]
            [HttpGet("/not-restricted/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        static HttpClient CreateClient(Action<ViewModelCompositionOptions> registerHandlers) =>
            new SelfContainedWebApplicationFactoryWithWebHost<Get_with_allow_anonymous_on_some_handlers>
            (
                configureServices: services =>
                {
                    services.AddAuthorization();
                    services.AddAuthentication("BasicAuthentication")
                        .AddScheme<DelegateAuthenticationSchemeOptions, TestAuthenticationHandler>("BasicAuthentication", options =>
                        {
                            options.OnAuthenticate = request => Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
                        });
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        registerHandlers(options);
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

        [Fact]
        public async Task Allow_anonymous_on_a_handler_does_not_disable_authorize_on_another()
        {
            var client = CreateClient(options =>
            {
                options.RegisterCompositionHandler<AuthorizeHandler>();
                options.RegisterCompositionHandler<AllowAnonymousOnRestrictedRouteHandler>();
            });

            var response = await client.GetAsync("/restricted/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Allow_anonymous_is_kept_when_no_handler_requires_authorization()
        {
            var client = CreateClient(options =>
            {
                options.RegisterCompositionHandler<NotRestrictedHandler>();
                options.RegisterCompositionHandler<AllowAnonymousOnNotRestrictedRouteHandler>();
            });

            var response = await client.GetAsync("/not-restricted/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
