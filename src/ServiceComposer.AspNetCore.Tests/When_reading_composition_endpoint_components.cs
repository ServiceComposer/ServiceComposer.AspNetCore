using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    // Top level: the generator can't copy attributes declared in nested types onto contract-less handlers.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ComponentMarkerAttribute(string value) : Attribute
    {
        public string Value { get; } = value;
    }

    public class When_reading_composition_endpoint_components
    {
        [ComponentMarker("handler class")]
        class OrderHandler : ICompositionRequestsHandler
        {
            [ComponentMarker("handler method")]
            [HttpGet("/components/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class OrderSubscriber : ICompositionEventsSubscriber
        {
            [HttpGet("/components/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher) { }
        }

        class OrderViewModelFactory : IEndpointScopedViewModelFactory
        {
            [HttpGet("/components/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext) => new { };
        }

        [ComponentMarker("contract-less class")]
        [CompositionHandler]
        public class ContractlessHandler
        {
            [ComponentMarker("contract-less method by id")]
            [HttpGet("/contract-less-components/{id}")]
            public Task Get(int id) => Task.CompletedTask;

            [ComponentMarker("contract-less method by name")]
            [HttpGet("/contract-less-components/by-name/{name}")]
            public Task Get(string name) => Task.CompletedTask;
        }

        static async Task<ConcurrentDictionary<string, CompositionEndpointComponents>> ComponentsByRoute(Action<ViewModelCompositionOptions> register, params string[] requests)
        {
            var componentsByRoute = new ConcurrentDictionary<string, CompositionEndpointComponents>();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_reading_composition_endpoint_components>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        register(options);
                    });
                    services.AddRouting();
                    services.AddControllers();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers().Add(endpointBuilder =>
                        componentsByRoute[endpointBuilder.DisplayName!] = endpointBuilder.Metadata.OfType<CompositionEndpointComponents>().Single()));
                }
            ).CreateClient();

            foreach (var request in requests)
            {
                await client.GetAsync(request);
            }

            return componentsByRoute;
        }

        static string[] Markers(System.Collections.Generic.IEnumerable<object> attributes) =>
            attributes.OfType<ComponentMarkerAttribute>().Select(marker => marker.Value).ToArray();

        [Fact]
        public async Task Each_component_has_its_kind_method_and_attributes()
        {
            var componentsByRoute = await ComponentsByRoute(options =>
            {
                options.RegisterCompositionHandler<OrderHandler>();
                options.RegisterCompositionHandler<OrderSubscriber>();
                options.RegisterCompositionHandler<OrderViewModelFactory>();
            }, "/components/1");

            var components = componentsByRoute["components/{id}"].Components;

            var handler = Assert.Single(components, component => component.Kind == CompositionComponentKind.RequestsHandler);
            Assert.Equal(typeof(OrderHandler), handler.UserType);
            Assert.Equal(typeof(OrderHandler), handler.ComponentType);
            Assert.Equal(nameof(OrderHandler.Handle), handler.Method.Name);
            Assert.Equal(new[] { "handler class" }, Markers(handler.TypeAttributes));
            Assert.Equal(new[] { "handler method" }, Markers(handler.MethodAttributes));

            var subscriber = Assert.Single(components, component => component.Kind == CompositionComponentKind.EventsSubscriber);
            Assert.Equal(typeof(OrderSubscriber), subscriber.UserType);
            Assert.Equal(nameof(OrderSubscriber.Subscribe), subscriber.Method.Name);

            var factory = Assert.Single(components, component => component.Kind == CompositionComponentKind.ViewModelFactory);
            Assert.Equal(typeof(OrderViewModelFactory), factory.UserType);
            Assert.Equal(nameof(OrderViewModelFactory.CreateViewModel), factory.Method.Name);
        }

        [Fact]
        public async Task Contract_less_handlers_point_to_the_user_class_and_method()
        {
            var componentsByRoute = await ComponentsByRoute(options =>
            {
                options.RegisterCompositionHandler<ContractlessHandler>();
                options.RegisterCompositionHandler<Generated.When_reading_composition_endpoint_components_ContractlessHandler_Get_int_id>();
                options.RegisterCompositionHandler<Generated.When_reading_composition_endpoint_components_ContractlessHandler_Get_string_name>();
            }, "/contract-less-components/1");

            var byId = Assert.Single(componentsByRoute["contract-less-components/{id}"].Components);
            Assert.Equal(CompositionComponentKind.RequestsHandler, byId.Kind);
            Assert.Equal(typeof(ContractlessHandler), byId.UserType);
            Assert.Equal(typeof(Generated.When_reading_composition_endpoint_components_ContractlessHandler_Get_int_id), byId.ComponentType);
            Assert.Equal(typeof(int), Assert.Single(byId.Method.GetParameters()).ParameterType);
            Assert.Equal(new[] { "contract-less class" }, Markers(byId.TypeAttributes));
            Assert.Equal(new[] { "contract-less method by id" }, Markers(byId.MethodAttributes));

            var byName = Assert.Single(componentsByRoute["contract-less-components/by-name/{name}"].Components);
            Assert.Equal(typeof(string), Assert.Single(byName.Method.GetParameters()).ParameterType);
            Assert.Equal(new[] { "contract-less method by name" }, Markers(byName.MethodAttributes));
        }
    }
}
