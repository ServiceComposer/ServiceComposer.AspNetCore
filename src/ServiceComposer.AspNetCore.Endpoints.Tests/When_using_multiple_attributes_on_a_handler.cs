using System.Dynamic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_using_multiple_attributes_on_a_handler
    {
        public class MultipleAttributesOfDifferentTypesHandler : ICompositionRequestsHandler
        {
            [HttpPost("/multiple/attributes")]
            [HttpGet("/multiple/attributes/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.RequestPath = request.Path;

                return Task.CompletedTask;
            }
        }

        public class MultipleGetAttributesHandler : ICompositionRequestsHandler
        {
            [HttpGet("/multiple/attributes")]
            [HttpGet("/multiple/attributes/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.RequestPath = request.Path;

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task If_attribues_are_of_different_types_handler_should_be_invoked_for_all_routes()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_custom_services_registration_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<MultipleAttributesOfDifferentTypesHandler>();
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

            var json = "{}";
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;

            // Act
            var postResponse = await client.PostAsync("/multiple/attributes", stringContent);
            var getResponse = await client.GetAsync("/multiple/attributes/2");

            // Assert
            //Assert.True(composedResponse.IsSuccessStatusCode);
        }

        [Fact]
        public async Task If_attribues_are_of_the_same_type_handler_should_be_invoked_for_all_routes()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_custom_services_registration_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<MultipleGetAttributesHandler>();
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
            var composedResponse1 = await client.GetAsync("/multiple/attributes");
            var composedResponse2 = await client.GetAsync("/multiple/attributes/2");

            // Assert
            //Assert.True(composedResponse.IsSuccessStatusCode);
        }
    }
}