using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using TestClassLibraryWithHandlers;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_generated_components_providers
    {
        static SelfContainedWebApplicationFactoryWithWebHost<When_using_generated_components_providers> CreateFactory(Action<IServiceProvider> onConfigure = null)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<When_using_generated_components_providers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterComponentsFrom<TestClassLibraryWithHandlers.GeneratedCompositionComponentsProvider>();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                    });
                    services.AddRouting();
                    services.AddControllers();
                    // TestContractLessCompositionHandler uses IHttpContextAccessor via constructor injection.
                    services.AddHttpContextAccessor();
                },
                configure: app =>
                {
                    onConfigure?.Invoke(app.ApplicationServices);
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            );
        }

        [Fact]
        public void The_assembly_points_to_the_generated_provider()
        {
            var attribute = typeof(TestModel).Assembly.GetCustomAttribute<CompositionComponentsProviderAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(typeof(TestClassLibraryWithHandlers.GeneratedCompositionComponentsProvider), attribute.ProviderType);
        }

        [Fact]
        public async Task Requests_handlers_and_events_handlers_are_registered()
        {
            var expectedValue = 1;
            var client = CreateFactory().CreateClient();

            var response = await client.GetAsync($"/raise-event/{expectedValue}");
            var responseObj = JsonSerializer.Deserialize<TestModelWithEvent>(await response.Content.ReadAsStringAsync());

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(expectedValue, responseObj.Id);
            Assert.Equal(expectedValue, responseObj.TheIdFromTheEvent);
        }

        [Fact]
        public async Task Contract_less_handlers_are_registered()
        {
            var expectedValue = 1;
            var client = CreateFactory().CreateClient();

            var response = await client.GetAsync($"/contract-less-handler/{expectedValue}");
            var responseObj = JsonSerializer.Deserialize<TestModel>(await response.Content.ReadAsStringAsync());

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(expectedValue, responseObj.Value);
        }

        [Fact]
        public void Preview_handlers_are_registered()
        {
            IServiceProvider container = null;
            CreateFactory(sp => container = sp).CreateClient();

            var previewHandlers = container.GetServices<IViewModelPreviewHandler>();

            Assert.Single(previewHandlers.OfType<TestPreviewHandler>());
        }
    }
}
