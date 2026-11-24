using System;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    namespace Controllers
    {
        [Route("/api/authorization-over-controllers")]
        public class AuthorizationOverControllersController : ControllerBase
        {
            static IActionResult Model()
            {
                dynamic model = new ExpandoObject();
                model.FromController = true;
                return new OkObjectResult(model);
            }

            [HttpGet("handler-method-authorize")]
            public IActionResult HandlerMethodAuthorize() => Model();

            [HttpGet("handler-class-authorize")]
            public IActionResult HandlerClassAuthorize() => Model();

            [AllowAnonymous]
            [HttpGet("controller-allow-anonymous")]
            public IActionResult ControllerAllowAnonymous() => Model();

            [Authorize]
            [HttpGet("controller-authorize")]
            public IActionResult ControllerAuthorize() => Model();

            [HttpGet("no-authorization")]
            public IActionResult NoAuthorization() => Model();
        }
    }

    public class When_composing_over_controllers_with_authorization
    {
        const string Prefix = "/api/authorization-over-controllers";

        class MethodAuthorizeHandler : ICompositionRequestsHandler
        {
            [Authorize]
            [HttpGet(Prefix + "/handler-method-authorize")]
            [HttpGet(Prefix + "/controller-allow-anonymous")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().FromHandler = true;
                return Task.CompletedTask;
            }
        }

        [Authorize]
        class ClassAuthorizeHandler : ICompositionRequestsHandler
        {
            [HttpGet(Prefix + "/handler-class-authorize")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().FromHandler = true;
                return Task.CompletedTask;
            }
        }

        class AllowAnonymousHandler : ICompositionRequestsHandler
        {
            [AllowAnonymous]
            [HttpGet(Prefix + "/controller-authorize")]
            [HttpGet(Prefix + "/no-authorization")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().FromHandler = true;
                return Task.CompletedTask;
            }
        }

        const string AuthenticatedHeader = "X-Test-Authenticated";

        static HttpClient CreateClient(
            ILoggerFactory loggerFactory = null,
            Action<ControllerActionEndpointConventionBuilder> configureControllers = null) =>
            new SelfContainedWebApplicationFactoryWithWebHost<When_composing_over_controllers_with_authorization>
            (
                configureServices: services =>
                {
                    services.AddAuthorization();
                    services.AddAuthentication("Test")
                        .AddScheme<DelegateAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options =>
                        {
                            options.OnAuthenticate = request => Task.FromResult(request.Headers.ContainsKey(AuthenticatedHeader)
                                ? AuthenticateResult.Success(new AuthenticationTicket(
                                    new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], "Test")), "Test"))
                                : AuthenticateResult.Fail("Not authenticated"));
                        });
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<MethodAuthorizeHandler>();
                        options.RegisterCompositionHandler<ClassAuthorizeHandler>();
                        options.RegisterCompositionHandler<AllowAnonymousHandler>();
                    });
                    services.AddRouting();
                    services.AddControllers().AddNewtonsoftJson();
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
                    app.UseEndpoints(builder =>
                    {
                        var controllers = builder.MapControllers().WithCompositionOverControllers();
                        configureControllers?.Invoke(controllers);
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

        static Task<HttpResponseMessage> Get(HttpClient client, string path, bool authenticated = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, Prefix + path);
            if (authenticated)
            {
                request.Headers.Add(AuthenticatedHeader, "true");
            }

            return client.SendAsync(request);
        }

        static async Task AssertComposed(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.GetValue("FromController", StringComparison.OrdinalIgnoreCase)?.Value<bool>());
            Assert.True(body.GetValue("FromHandler", StringComparison.OrdinalIgnoreCase)?.Value<bool>());
        }

        [Theory]
        [InlineData("/handler-method-authorize")]
        [InlineData("/handler-class-authorize")]
        public async Task Handlers_authorization_applies_to_the_controller_endpoint(string path)
        {
            var client = CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, path)).StatusCode);
            await AssertComposed(await Get(client, path, authenticated: true));
        }

        [Fact]
        public async Task Allow_anonymous_on_the_action_does_not_disable_handlers_authorization()
        {
            var client = CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, "/controller-allow-anonymous")).StatusCode);
            await AssertComposed(await Get(client, "/controller-allow-anonymous", authenticated: true));
        }

        [Fact]
        public async Task Allow_anonymous_on_handlers_does_not_disable_the_controller_authorization()
        {
            var client = CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await Get(client, "/controller-authorize")).StatusCode);
            await AssertComposed(await Get(client, "/controller-authorize", authenticated: true));
        }

        [Fact]
        public async Task Allow_anonymous_is_kept_when_nothing_requires_authorization()
        {
            var client = CreateClient(configureControllers: controllers =>
                controllers.RequireAuthorization().WithMetadata(new AllowAnonymousAttribute()));

            await AssertComposed(await Get(client, "/no-authorization"));
        }

        const string IgnoredAllowAnonymousWarning = "[AllowAnonymous] on {ComponentsAllowingAnonymous} is ignored for {HttpMethods} {Template}, " +
                                                    "since authorization is required by {ComponentsRequiringAuthorization}: the most restrictive requirements apply to the route.";

        [Fact]
        public async Task Ignoring_allow_anonymous_on_the_action_logs_a_warning()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var client = CreateClient(loggerFactory);

            await Get(client, "/controller-allow-anonymous");

            var log = Assert.Single(loggerFactory.Sink.LogEntries, l => l.LogLevel == LogLevel.Warning
                && l.OriginalFormat == IgnoredAllowAnonymousWarning
                && (string)l.Properties.Single(p => p.Key == "Template").Value == "api/authorization-over-controllers/controller-allow-anonymous");
            Assert.Contains(nameof(Controllers.AuthorizationOverControllersController.ControllerAllowAnonymous),
                (string)log.Properties.Single(p => p.Key == "ComponentsAllowingAnonymous").Value);
            Assert.Equal(nameof(MethodAuthorizeHandler), log.Properties.Single(p => p.Key == "ComponentsRequiringAuthorization").Value);
        }

        [Fact]
        public async Task Ignoring_allow_anonymous_on_handlers_logs_a_warning()
        {
            var loggerFactory = TestLoggerFactory.Create();
            var client = CreateClient(loggerFactory);

            await Get(client, "/controller-authorize");

            var log = Assert.Single(loggerFactory.Sink.LogEntries, l => l.LogLevel == LogLevel.Warning
                && l.OriginalFormat == IgnoredAllowAnonymousWarning
                && (string)l.Properties.Single(p => p.Key == "Template").Value == "api/authorization-over-controllers/controller-authorize");
            Assert.Equal(nameof(AllowAnonymousHandler), log.Properties.Single(p => p.Key == "ComponentsAllowingAnonymous").Value);
            Assert.Contains(nameof(Controllers.AuthorizationOverControllersController.ControllerAuthorize),
                (string)log.Properties.Single(p => p.Key == "ComponentsRequiringAuthorization").Value);
        }

        [Fact]
        public async Task Conventions_see_the_components_composing_over_the_controller_endpoint()
        {
            CompositionEndpointComponents seen = null;
            var client = CreateClient(configureControllers: controllers => controllers.Add(endpointBuilder =>
            {
                if (endpointBuilder.DisplayName?.Contains(nameof(Controllers.AuthorizationOverControllersController.HandlerClassAuthorize)) == true)
                {
                    seen = endpointBuilder.Metadata.OfType<CompositionEndpointComponents>().SingleOrDefault();
                }
            }));

            await Get(client, "/handler-class-authorize", authenticated: true);

            Assert.NotNull(seen);
            var component = Assert.Single(seen.Components);
            Assert.Equal(typeof(ClassAuthorizeHandler), component.UserType);
        }

        [Fact]
        public void Fails_at_startup_when_the_components_metadata_is_not_on_the_controller_endpoint()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => CreateClient(configureControllers: controllers => controllers.Add(endpointBuilder =>
            {
                foreach (var components in endpointBuilder.Metadata.OfType<CompositionEndpointComponents>().ToArray())
                {
                    endpointBuilder.Metadata.Remove(components);
                }
            })));

            Assert.Contains("couldn't be applied to the controller endpoint", exception.Message);
        }
    }
}
