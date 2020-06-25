using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_using_custom_services_registration_handlers
    {
        public class TestNoOpHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Handlers_should_be_registered_as_expected()
        {
            // Arrange
            bool invokedCustomhandler = false;
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_custom_services_registration_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.AddServicesConfigurationHandler(typeof(TestNoOpHandler), (type, serviceCollection) =>
                        {
                            invokedCustomhandler = true;
                            serviceCollection.AddTransient(type);
                        });
                        options.RegisterCompositionHandler<TestNoOpHandler>();
                    });
                    services.AddControllers();
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapCompositionHandlers();
                        builder.MapControllers();
                    });
                }
            ).CreateClient();

            // Act
            var composedResponse = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(composedResponse.IsSuccessStatusCode);
            Assert.True(invokedCustomhandler);
        }
    }
}