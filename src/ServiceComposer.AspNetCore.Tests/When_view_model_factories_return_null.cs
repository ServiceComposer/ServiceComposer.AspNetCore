using System;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_view_model_factories_return_null
    {
        public class SourceViewModel
        {
            public string Source { get; set; }
        }

        class TestHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                if (vm is ExpandoObject)
                {
                    vm.Source = "dynamic";
                }

                return Task.CompletedTask;
            }
        }

        class NullEndpointScopedFactory : IEndpointScopedViewModelFactory
        {
            [HttpGet("/sample/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext) => null;
        }

        class GlobalFactory : IViewModelFactory
        {
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
                => new SourceViewModel { Source = "global" };
        }

        class NullGlobalFactory : IViewModelFactory
        {
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext) => null;
        }

        static async Task<string> GetSource(Action<ViewModelCompositionOptions> registerFactories, ILoggerFactory loggerFactory = null)
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestHandler>();
                        registerFactories(options);
                    });
                    services.AddRouting();
                    if (loggerFactory != null)
                    {
                        services.Replace(ServiceDescriptor.Singleton(loggerFactory));
                    }
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            var response = await client.GetAsync("/sample/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseObj = JObject.Parse(await response.Content.ReadAsStringAsync());
            return responseObj.SelectToken("source")?.Value<string>();
        }

        [Fact]
        public async Task Endpoint_scoped_factory_falls_through_to_the_global_one()
        {
            var source = await GetSource(options =>
            {
                options.RegisterEndpointScopedViewModelFactory<NullEndpointScopedFactory>();
                options.RegisterGlobalViewModelFactory<GlobalFactory>();
            });

            Assert.Equal("global", source);
        }

        [Fact]
        public async Task Endpoint_scoped_factory_falls_through_to_the_dynamic_view_model_without_a_global_one()
        {
            var source = await GetSource(options =>
            {
                options.RegisterEndpointScopedViewModelFactory<NullEndpointScopedFactory>();
            });

            Assert.Equal("dynamic", source);
        }

        [Fact]
        public async Task Global_factory_falls_through_to_the_dynamic_view_model()
        {
            var source = await GetSource(options =>
            {
                options.RegisterGlobalViewModelFactory<NullGlobalFactory>();
            });

            Assert.Equal("dynamic", source);
        }

        [Fact]
        public async Task Both_factories_fall_through_to_the_dynamic_view_model()
        {
            var source = await GetSource(options =>
            {
                options.RegisterEndpointScopedViewModelFactory<NullEndpointScopedFactory>();
                options.RegisterGlobalViewModelFactory<NullGlobalFactory>();
            });

            Assert.Equal("dynamic", source);
        }

        [Fact]
        public async Task Logs_debug_when_a_factory_falls_through()
        {
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

            await GetSource(options =>
            {
                options.RegisterEndpointScopedViewModelFactory<NullEndpointScopedFactory>();
                options.RegisterGlobalViewModelFactory<GlobalFactory>();
            }, loggerFactory);

            var log = loggerFactory.Sink.LogEntries
                .Single(l =>
                {
                    try { return l.OriginalFormat == "View model factory {ViewModelFactory} returned null for {Method} {Path}, falling back to the next factory."; }
                    catch { return false; }
                });
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(nameof(NullEndpointScopedFactory), log.Properties.Single(p => p.Key == "ViewModelFactory").Value);
            Assert.Equal("GET", log.Properties.Single(p => p.Key == "Method").Value);
            Assert.Equal("/sample/1", log.Properties.Single(p => p.Key == "Path").Value);
        }
    }
}
