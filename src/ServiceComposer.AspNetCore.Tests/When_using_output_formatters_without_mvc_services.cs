using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_output_formatters_without_mvc_services
    {
        class TestHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.Value = "sample";
                return Task.CompletedTask;
            }
        }

        static SelfContainedWebApplicationFactoryWithWebHost<Dummy> CompositionFactory(bool addControllers)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestHandler>();
                        options.ResponseSerialization.UseOutputFormatters = true;
                    });
                    services.AddRouting();
                    if (addControllers)
                    {
                        services.AddControllers();
                    }
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            );
        }

        [Fact]
        public void Mapping_composition_handlers_throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => CompositionFactory(addControllers: false).CreateClient());

            Assert.Contains("ResponseSerialization.UseOutputFormatters", ex.Message);
            Assert.Contains("AddControllers()", ex.Message);
        }

        [Fact]
        public async Task Composition_handlers_work_with_mvc_services()
        {
            var client = CompositionFactory(addControllers: true).CreateClient();

            var response = await client.GetAsync("/sample/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void Mapping_a_scatter_gather_route_throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
                (
                    configureServices: services => services.AddRouting(),
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder => builder.MapScatterGather("/items", new ScatterGatherOptions
                        {
                            UseOutputFormatters = true,
                            Gatherers = []
                        }));
                    }
                ).CreateClient());

            Assert.Contains("UseOutputFormatters of the scatter-gather route /items", ex.Message);
            Assert.Contains("AddControllers()", ex.Message);
        }

        [Fact]
        public void Mapping_scatter_gather_routes_from_configuration_throws()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Routes:0:Template"] = "/items",
                ["Routes:0:UseOutputFormatters"] = "true",
                ["Routes:0:Gatherers:0:Key"] = "Source",
                ["Routes:0:Gatherers:0:DestinationUrl"] = "http://localhost/upstream/source"
            }).Build();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
                (
                    configureServices: services =>
                    {
                        services.AddRouting();
                        services.AddScatterGather();
                    },
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder => builder.MapScatterGather(config.GetSection("Routes")));
                    }
                ).CreateClient());

            Assert.Contains("UseOutputFormatters of the scatter-gather route /items", ex.Message);
        }
    }
}
