using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlers
{
    public class When_using_custom_services_registration_handlers
    {
        [CompositionHandler]
        public class TestNoOpCompositionHandler
        {
            [HttpGet("/sample/{id}")]
            public Task NoOp()
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Handlers_should_be_registered_as_expected()
        {
            // Arrange
            var invokedCustomHandler = false;
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_custom_services_registration_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.AddServicesConfigurationHandler(typeof(TestNoOpCompositionHandler), (type, serviceCollection) =>
                        {
                            invokedCustomHandler = true;
                            serviceCollection.AddTransient(type);
                        });
                        options.RegisterCompositionHandler<TestNoOpCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_custom_services_registration_handlers_TestNoOpCompositionHandler_NoOp>();
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
            Assert.True(invokedCustomHandler);
        }
    }
}