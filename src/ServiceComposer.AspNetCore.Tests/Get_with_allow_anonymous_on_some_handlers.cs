using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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

        static HttpClient CreateClient(Action<ViewModelCompositionOptions> registerHandlers, ILoggerFactory loggerFactory = null) =>
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
                    if (loggerFactory != null)
                    {
                        services.Replace(ServiceDescriptor.Singleton(loggerFactory));
                    }
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

        const string IgnoredAllowAnonymousWarning = "[AllowAnonymous] on {ComponentsAllowingAnonymous} is ignored for {HttpMethods} {Template}, " +
                                                    "since authorization is required by {ComponentsRequiringAuthorization}: the most restrictive requirements apply to the route.";

        [Fact]
        public async Task Ignoring_allow_anonymous_logs_a_warning()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var client = CreateClient(options =>
            {
                options.RegisterCompositionHandler<AuthorizeHandler>();
                options.RegisterCompositionHandler<AllowAnonymousOnRestrictedRouteHandler>();
            }, loggerFactory);

            await client.GetAsync("/restricted/1");

            var log = Assert.Single(loggerFactory.Sink.LogEntries, l => l.LogLevel == LogLevel.Warning && l.OriginalFormat == IgnoredAllowAnonymousWarning);
            Assert.Equal(nameof(AllowAnonymousOnRestrictedRouteHandler), log.Properties.Single(p => p.Key == "ComponentsAllowingAnonymous").Value);
            Assert.Equal(nameof(AuthorizeHandler), log.Properties.Single(p => p.Key == "ComponentsRequiringAuthorization").Value);
            Assert.Equal("GET", log.Properties.Single(p => p.Key == "HttpMethods").Value);
            Assert.Equal("restricted/{id}", log.Properties.Single(p => p.Key == "Template").Value);
        }

        [Fact]
        public async Task Keeping_allow_anonymous_logs_no_warning()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var client = CreateClient(options =>
            {
                options.RegisterCompositionHandler<NotRestrictedHandler>();
                options.RegisterCompositionHandler<AllowAnonymousOnNotRestrictedRouteHandler>();
            }, loggerFactory);

            await client.GetAsync("/not-restricted/1");

            Assert.DoesNotContain(loggerFactory.Sink.LogEntries, l => l.LogLevel == LogLevel.Warning && l.OriginalFormat == IgnoredAllowAnonymousWarning);
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
