using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_registering_components_providers
    {
        class CountingHandler : ICompositionRequestsHandler
        {
            [HttpGet("/providers/sample")]
            public Task Handle(HttpRequest request)
            {
                var vm = (IDictionary<string, object>)request.GetComposedResponseModel();
                vm["Invocations"] = vm.TryGetValue("Invocations", out var current) ? (int)current + 1 : 1;
                return Task.CompletedTask;
            }
        }

        class MarkerService;

        class Customization : IViewModelCompositionOptionsCustomization
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                // Throws if the customization is applied more than once
                options.AddServicesConfigurationHandler(typeof(CountingHandler), (type, services) =>
                {
                    services.AddSingleton<MarkerService>();
                    services.AddTransient(type);
                });
            }
        }

        class Provider : ICompositionComponentsProvider
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                new Customization().Customize(options);
            }

            public void RegisterComponents(ViewModelCompositionOptions options)
            {
                options.RegisterCompositionHandler<CountingHandler>();
            }
        }

        class ProviderRegisteringOtherProviders : ICompositionComponentsProvider
        {
            public void Customize(ViewModelCompositionOptions options)
            {
                options.RegisterComponentsFrom<Provider>();
            }

            public void RegisterComponents(ViewModelCompositionOptions options) { }
        }

        static SelfContainedWebApplicationFactoryWithWebHost<When_registering_components_providers> CreateFactory(Action<ViewModelCompositionOptions> config)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<When_registering_components_providers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(config);
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            );
        }

        static async Task<JObject> GetSample(SelfContainedWebApplicationFactoryWithWebHost<When_registering_components_providers> factory)
        {
            var client = factory.CreateClient();
            var response = await client.GetAsync("/providers/sample");
            Assert.True(response.IsSuccessStatusCode);
            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Components_are_registered_without_assembly_scanning()
        {
            await using var factory = CreateFactory(options =>
            {
                options.AssemblyScanner.Disable();
                options.RegisterComponentsFrom<Provider>();
            });

            var responseObj = await GetSample(factory);

            Assert.Equal(1, responseObj.SelectToken("invocations")?.Value<int>());
            Assert.NotNull(factory.Services.GetService<MarkerService>());
        }

        [Fact]
        public async Task The_provider_assembly_is_not_scanned()
        {
            await using var factory = CreateFactory(options =>
            {
                // Without excluding the provider assembly, scanning would register
                // CountingHandler again and apply Customization again (which throws)
                options.TypesFilter = type => type.IsNestedTypeOf<When_registering_components_providers>()
                                              && type != typeof(ProviderRegisteringOtherProviders);
                options.RegisterComponentsFrom<Provider>();
            });

            var responseObj = await GetSample(factory);

            Assert.Equal(1, responseObj.SelectToken("invocations")?.Value<int>());
        }

        [Fact]
        public async Task Registering_the_same_provider_twice_has_no_effect()
        {
            await using var factory = CreateFactory(options =>
            {
                options.AssemblyScanner.Disable();
                options.RegisterComponentsFrom<Provider>();
                options.RegisterComponentsFrom<Provider>();
            });

            var responseObj = await GetSample(factory);

            Assert.Equal(1, responseObj.SelectToken("invocations")?.Value<int>());
        }

        [Fact]
        public void Registering_providers_during_initialization_throws()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<InvalidOperationException>(() => services.AddViewModelComposition(options =>
            {
                options.AssemblyScanner.Disable();
                options.RegisterComponentsFrom<ProviderRegisteringOtherProviders>();
            }));

            Assert.Contains(nameof(ViewModelCompositionOptions.RegisterComponentsFrom), exception.Message);
        }
    }
}
