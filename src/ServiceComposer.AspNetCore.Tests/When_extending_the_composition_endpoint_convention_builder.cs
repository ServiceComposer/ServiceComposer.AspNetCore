using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    // A library's extension method on CompositionEndpointConventionBuilder is picked over another's
    // generic one, with the same name, on IEndpointConventionBuilder: C# prefers the non-generic overload.
    static class GenericEndpointConventionBuilderExtensions
    {
        public static TBuilder WithMarker<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        {
            builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(new Marker("generic")));
            return builder;
        }
    }

    static class CompositionEndpointConventionBuilderExtensions
    {
        public static CompositionEndpointConventionBuilder WithMarker(this CompositionEndpointConventionBuilder builder)
        {
            builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(new Marker("composition")));
            return builder;
        }
    }

    record Marker(string Source);

    public class When_extending_the_composition_endpoint_convention_builder
    {
        class Handler : ICompositionRequestsHandler
        {
            [HttpGet("/marked/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.Marker = request.HttpContext.GetEndpoint()?.Metadata.GetMetadata<Marker>()?.Source;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task The_composition_specific_extension_method_is_picked_and_its_conventions_apply()
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_extending_the_composition_endpoint_convention_builder>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<Handler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers().WithMarker());
                }
            ).CreateClient();
            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");

            var response = await client.GetStringAsync("/marked/1");

            Assert.Contains("\"Marker\":\"composition\"", response);
        }
    }
}
