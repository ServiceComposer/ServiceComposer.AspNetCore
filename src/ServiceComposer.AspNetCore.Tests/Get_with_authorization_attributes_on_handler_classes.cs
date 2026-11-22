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
    public class Get_with_authorization_attributes_on_handler_classes
    {
        [Authorize]
        class ClassLevelAuthorizeHandler : ICompositionRequestsHandler
        {
            [HttpGet("/class-level-authorize/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        [Authorize]
        class ClassLevelAuthorizeMethodLevelAllowAnonymousHandler : ICompositionRequestsHandler
        {
            [AllowAnonymous]
            [HttpGet("/method-level-allow-anonymous/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        [Authorize]
        [CompositionHandler]
        public class ClassLevelAuthorizeContractlessHandler
        {
            [HttpGet("/contract-less-class-level-authorize/{id}")]
            public Task Handle(int id) => Task.CompletedTask;
        }

        static HttpClient CreateClient(Action<ViewModelCompositionOptions> registerHandlers) =>
            new SelfContainedWebApplicationFactoryWithWebHost<Get_with_authorization_attributes_on_handler_classes>
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
                    services.AddControllers();
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
        public async Task Authorize_on_the_class_is_enforced()
        {
            var client = CreateClient(options => options.RegisterCompositionHandler<ClassLevelAuthorizeHandler>());

            var response = await client.GetAsync("/class-level-authorize/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Allow_anonymous_on_the_method_wins_over_authorize_on_the_class()
        {
            var client = CreateClient(options => options.RegisterCompositionHandler<ClassLevelAuthorizeMethodLevelAllowAnonymousHandler>());

            var response = await client.GetAsync("/method-level-allow-anonymous/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Authorize_on_a_contract_less_handler_class_is_enforced()
        {
            var client = CreateClient(options =>
            {
                options.RegisterCompositionHandler<ClassLevelAuthorizeContractlessHandler>();
                options.RegisterCompositionHandler<Generated.Get_with_authorization_attributes_on_handler_classes_ClassLevelAuthorizeContractlessHandler_Handle_int_id>();
            });

            var response = await client.GetAsync("/contract-less-class-level-authorize/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
