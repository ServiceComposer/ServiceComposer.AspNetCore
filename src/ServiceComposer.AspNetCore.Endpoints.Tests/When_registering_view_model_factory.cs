using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_registering_view_model_factory
    {
        class CustomViewModel
        {
            public string AValue { get; set; }
        }

        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel<CustomViewModel>();
                vm.AValue = "some value";
                return Task.CompletedTask;
            }
        }

        class TestViewModelFactory : IViewModelFactory
        {
            public bool Invoked { get; private set; }
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                Invoked = true;
                return new CustomViewModel();
            }
        }

        [Fact]
        public async Task ViewModel_is_created_using_custom_factory()
        {
            // Arrange
            var factory = new TestViewModelFactory();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_registering_view_model_factory>
            (
                configureServices: services =>
                {
                    services.AddSingleton<IViewModelFactory>(factory);
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            // Act
            var response = await client.GetAsync("/sample/1");

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("some value", responseObj?.SelectToken(nameof(CustomViewModel.AValue))?.Value<string>());
            Assert.True(factory.Invoked);
        }
    }
}