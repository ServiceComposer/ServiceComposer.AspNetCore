using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class Get_with_2_handlers_using_multiple_endpoint_scoped_view_model_factories
    {
        class TestFactory : IEndpointScopedViewModelFactory
        {
            [HttpGet("/sample/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return default;
            }
        }

        class TestFactory2 : IEndpointScopedViewModelFactory
        {
            [HttpGet("/sample/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return default;
            }
        }

        [Fact]
        public void Fails_when_factories_are_scoped_to_the_same_endpoint()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                // Arrange
                var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
                (
                    configureServices: services =>
                    {
                        services.AddViewModelComposition(options =>
                        {
                            options.AssemblyScanner.Disable();
                            options.RegisterEndpointScopedViewModelFactory<TestFactory>();
                            options.RegisterEndpointScopedViewModelFactory<TestFactory2>();
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