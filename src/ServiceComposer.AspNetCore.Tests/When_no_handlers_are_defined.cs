using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_no_handlers_are_defined
    {
        [Fact]
        public async Task Should_return_404()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_no_handlers_are_defined>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.DisableAssemblyScanning();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/no-handlers-are-registered/1");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
