using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_using_assembly_scanner
    {
        [Fact]
        public async Task Matching_handlers_are_found()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition();
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        private static bool _invoked = false;
        class Customizations : IViewModelCompositionOptionsCustomization
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                _invoked = true;
            }
        }

        [Fact]
        public async Task Options_customization_are_invoked()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_assembly_scanner>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition();
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(_invoked);
        }
    }
}