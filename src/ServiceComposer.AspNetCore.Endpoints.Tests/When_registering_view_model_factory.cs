using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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

        class TestGetDynamicHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AValue = "some value";
                return Task.CompletedTask;
            }
        }

        class TestViewModelFactory : IViewModelFactory
        {
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return new CustomViewModel();
            }
        }

        class ViewModelFactoryThatReturnsNull : IViewModelFactory
        {
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return null;
            }
        }

        [Fact]
        public async Task ViewModel_is_created_using_custom_factory()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_registering_view_model_factory>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandler>();
                        options.RegisterGlobalViewModelFactory<TestViewModelFactory>();
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
        }

        [Fact]
        public async Task DynamicViewModel_is_created_if_factory_returns_null()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_registering_view_model_factory>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetDynamicHandler>();
                        options.RegisterGlobalViewModelFactory<ViewModelFactoryThatReturnsNull>();
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
            Assert.Equal("some value", responseObj?.SelectToken("AValue")?.Value<string>());
        }

        [Fact]
        public void An_exception_is_thrown_if_more_than_one_global_factory_is_registered()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                // Arrange
                var client = new SelfContainedWebApplicationFactoryWithWebHost<When_registering_view_model_factory>
                (
                    configureServices: services =>
                    {
                        services.AddViewModelComposition(options =>
                        {
                            options.AssemblyScanner.Disable();
                            options.RegisterCompositionHandler<TestGetHandler>();
                            options.RegisterGlobalViewModelFactory<TestViewModelFactory>();
                            options.RegisterGlobalViewModelFactory<TestViewModelFactory>();
                        });
                        services.AddRouting();
                    },
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder => builder.MapCompositionHandlers());
                    }
                ).CreateClient();
            });
        }
    }
}