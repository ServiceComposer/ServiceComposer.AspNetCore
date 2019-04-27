using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Xunit;
using ServiceComposer.AspNetCore.Testing;
using System;
using System.Linq;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_assembly_scanning
    {
        class SampleNeverInvokedHandler : IHandleRequests
        {
            public Task Handle(string requestId, RouteData routeData, HttpRequest request)
            {
                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return false;
            }
        }

        [Fact]
        public void Should_not_fail_due_to_invalid_assemblies()
        {
            // Arrange
            var factory = new SelfContainedWebApplicationFactoryWithWebHost<When_assembly_scanning>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition();
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            );
            factory.CreateClient();
        }

        [Fact]
        public void Should_not_register_duplicated_handlers()
        {
            IServiceProvider container = null;
            // Arrange
            var factory = new SelfContainedWebApplicationFactoryWithWebHost<When_assembly_scanning>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition();
                    services.AddRouting();
                },
                configure: app =>
                {
                    container= app.ApplicationServices;
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            );
            factory.CreateClient();

            var handler = container.GetServices<IInterceptRoutes>()
                .SingleOrDefault(svc => svc is SampleNeverInvokedHandler);

            Assert.NotNull(handler);
        }
    }
}
