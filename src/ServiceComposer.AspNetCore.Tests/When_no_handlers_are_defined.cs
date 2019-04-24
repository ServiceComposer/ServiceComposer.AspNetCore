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
    public class When_no_handlers_are_defined : IClassFixture<TestWebApplicationFactory<When_no_handlers_are_defined.Startup>>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddViewModelComposition(options =>
                {
                    options.DisableAssemblyScanning();
                });
                services.AddRouting();
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
            {
                app.RunCompositionGatewayWithDefaultRoutes();
            }
        }

        readonly TestWebApplicationFactory<Startup> _factory;

        public When_no_handlers_are_defined(TestWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Should_return_404()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/no-handlers-are-registered/1");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
