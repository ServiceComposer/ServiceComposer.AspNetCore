using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Net;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_no_matching_handlers_are_found : IClassFixture<TestWebApplicationFactory<When_no_matching_handlers_are_found.Startup>>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddViewModelComposition(options =>
                {
                    options.DisableAssemblyScanning();
                    options.RegisterRequestsHandler<NeverMatchingHandler>();
                });
                services.AddRouting();
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
            {
                app.RunCompositionGatewayWithDefaultRoutes();
            }
        }

        class NeverMatchingHandler : IHandleRequests
        {
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                throw new System.NotImplementedException();
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return false;
            }
        }

        readonly TestWebApplicationFactory<Startup> _factory;

        public When_no_matching_handlers_are_found(TestWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Should_return_404()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/no-matching-handlers/1");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
