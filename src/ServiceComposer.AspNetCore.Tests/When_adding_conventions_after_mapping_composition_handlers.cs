using System.Net;
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
    namespace LateConventionsControllers
    {
        [Route("/api/late-conventions")]
        public class LateConventionsController : ControllerBase
        {
            [HttpGet("{id}")]
            public object Get(int id) => null;
        }
    }

    // Regression tests for https://github.com/ServiceComposer/ServiceComposer.AspNetCore/issues/1118
    public class When_adding_conventions_after_mapping_composition_handlers
    {
        class ControllerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/late-conventions/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        class CompositionOnlyHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/late-conventions-composition-only/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        const string MarkerHeader = "x-late-convention";

        static ValueTask<object> MarkResponse(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.Response.Headers[MarkerHeader] = "applied";
            return next(context);
        }

        static bool IsMarked(HttpResponseMessage response) => response.Headers.Contains(MarkerHeader);

        static SelfContainedWebApplicationFactoryWithWebHost<Dummy> CreateFactory(
            bool filterControllers, bool filterCompositionHandlers)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ControllerHandler>();
                        options.RegisterCompositionHandler<CompositionOnlyHandler>();
                        options.EnableCompositionOverControllers();
                    });
                    services.AddRouting();
                    services.AddControllers()
                        .AddNewtonsoftJson();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        var controllers = builder.MapControllers();
                        var compositionHandlers = builder.MapCompositionHandlers();

                        if (filterControllers)
                        {
                            controllers.AddEndpointFilter(MarkResponse);
                        }

                        if (filterCompositionHandlers)
                        {
                            compositionHandlers.AddEndpointFilter(MarkResponse);
                        }
                    });
                }
            );
        }

        [Fact]
        public async Task Controller_conventions_are_applied()
        {
            var client = CreateFactory(filterControllers: true, filterCompositionHandlers: false).CreateClient();

            var response = await client.GetAsync("/api/late-conventions/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(IsMarked(response));
        }

        [Fact]
        public async Task Composition_handlers_conventions_are_applied()
        {
            var client = CreateFactory(filterControllers: false, filterCompositionHandlers: true).CreateClient();

            var composedOverControllerResponse = await client.GetAsync("/api/late-conventions/1");
            var compositionOnlyResponse = await client.GetAsync("/api/late-conventions-composition-only/1");

            Assert.Equal(HttpStatusCode.OK, composedOverControllerResponse.StatusCode);
            Assert.False(IsMarked(composedOverControllerResponse));
            Assert.Equal(HttpStatusCode.OK, compositionOnlyResponse.StatusCode);
            Assert.True(IsMarked(compositionOnlyResponse));
        }
    }
}
