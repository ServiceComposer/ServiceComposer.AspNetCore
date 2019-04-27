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
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
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
        public void Without_any_filter_should_return_assemblies()
        {
            //arrange
            var scanner = new AssemblyScanner();

            //act
            var assemblies = scanner.Scan();

            Assert.NotEmpty(assemblies);
        }

        [Fact]
        public void With_exclude_all_filter_should_return_no_assemblies()
        {
            //arrange
            var scanner = new AssemblyScanner();
            scanner.AddAssemblyFilter(assemblyFullPath => AssemblyScanner.FilterResults.Exclude);
            //act
            var assemblies = scanner.Scan();

            Assert.Empty(assemblies);
        }

        [Fact]
        public void With_include_only_current_assembly_filter_should_return_1_assembly()
        {
            //arrange
            var currentAssemblyName = "ServiceComposer.AspNetCore.Tests.dll";
            var scanner = new AssemblyScanner();
            scanner.AddAssemblyFilter(assemblyFullPath =>
            {
                return assemblyFullPath.EndsWith(currentAssemblyName)
                    ? AssemblyScanner.FilterResults.Include
                    : AssemblyScanner.FilterResults.Exclude;
            });
            //act
            var assemblies = scanner.Scan();

            Assert.Single(assemblies);
        }

        [Fact]
        public void Should_register_non_duplicate_handlers()
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
