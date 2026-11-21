using System;
using System.Collections.Generic;
using System.Net;
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
    namespace EndpointFiltersControllers
    {
        [Route("/api/composition-over-controllers-endpoint-filters")]
        public class EndpointFiltersController : ControllerBase
        {
            [HttpGet("{id}")]
            public object Get(int id)
            {
                When_using_composition_over_controllers_with_endpoint_filters.Log(this, "action");
                return null;
            }

            [HttpGet("bad-request/{id}")]
            public object GetBadRequest(int id) => null;
        }
    }

    public class When_using_composition_over_controllers_with_endpoint_filters
    {
        // Each test uses its own log, keyed by the test's HttpContext
        const string LogKey = "test-log";

        internal static void Log(ControllerBase controller, string entry) => Log(controller.HttpContext, entry);

        static void Log(HttpContext context, string entry) => ((List<string>)context.Items[LogKey])!.Add(entry);

        class Handler : ICompositionRequestsHandler
        {
            [HttpGet("/api/composition-over-controllers-endpoint-filters/{id}")]
            public Task Handle(HttpRequest request)
            {
                Log(request.HttpContext, "handler");
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        class BadRequestHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/composition-over-controllers-endpoint-filters/bad-request/{id}")]
            public Task Handle(HttpRequest request)
            {
                request.SetActionResult(new BadRequestObjectResult("invalid"));
                return Task.CompletedTask;
            }
        }

        static SelfContainedWebApplicationFactoryWithWebHost<Dummy> CreateFactory(
            List<string> log,
            Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object>> outerFilter = null,
            bool mapCompositionHandlers = true,
            bool enableCompositionOverControllersGlobally = false,
            bool useCaseInsensitiveRouteMatching = true)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<Handler>();
                        options.RegisterCompositionHandler<BadRequestHandler>();
                        if (enableCompositionOverControllersGlobally)
                        {
                            // Covers the obsolete API while it's a warning
#pragma warning disable CS0618
                            options.EnableCompositionOverControllers();
#pragma warning restore CS0618
                        }
                    });
                    services.AddRouting();
                    services.AddControllers()
                        .AddNewtonsoftJson();
                },
                configure: app =>
                {
                    app.Use((context, next) =>
                    {
                        context.Items[LogKey] = log;
                        return next(context);
                    });
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        var controllers = builder.MapControllers();
                        if (outerFilter != null)
                        {
                            controllers.AddEndpointFilter(outerFilter);
                        }
                        controllers.WithCompositionOverControllers(useCaseInsensitiveRouteMatching);
                        if (mapCompositionHandlers)
                        {
                            builder.MapCompositionHandlers();
                        }
                    });
                }
            );
        }

        [Fact]
        public async Task Returns_the_composed_view_model()
        {
            var log = new List<string>();
            var client = CreateFactory(log).CreateClient();

            var response = await client.GetAsync("/api/composition-over-controllers-endpoint-filters/1");

            Assert.True(response.IsSuccessStatusCode);
            var responseObj = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("sample", responseObj.SelectToken("AString")?.Value<string>());
            // Composition runs once: the result filter skips composed requests
            Assert.Equal(["action", "handler"], log);
        }

        [Fact]
        public async Task Endpoint_filters_wrap_the_action_and_the_composition()
        {
            var log = new List<string>();
            object resultSeenByOuterFilter = null;
            var client = CreateFactory(log, outerFilter: async (context, next) =>
            {
                Log(context.HttpContext, "outer-before");
                resultSeenByOuterFilter = await next(context);
                Log(context.HttpContext, "outer-after");
                return resultSeenByOuterFilter;
            }).CreateClient();

            var response = await client.GetAsync("/api/composition-over-controllers-endpoint-filters/1");

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(["outer-before", "action", "handler", "outer-after"], log);
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(resultSeenByOuterFilter);
            Assert.NotNull(objectResult.Value);
        }

        [Fact]
        public async Task Endpoint_filters_see_the_action_result_set_by_handlers()
        {
            var log = new List<string>();
            object resultSeenByOuterFilter = null;
            var client = CreateFactory(log, outerFilter: async (context, next) =>
            {
                resultSeenByOuterFilter = await next(context);
                return resultSeenByOuterFilter;
            }).CreateClient();

            var response = await client.GetAsync("/api/composition-over-controllers-endpoint-filters/bad-request/1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsType<BadRequestObjectResult>(resultSeenByOuterFilter);
        }

        [Fact]
        public async Task Fails_if_composition_handlers_are_not_mapped()
        {
            var log = new List<string>();
            var client = CreateFactory(log, mapCompositionHandlers: false).CreateClient();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GetAsync("/api/composition-over-controllers-endpoint-filters/1"));

            Assert.Contains(nameof(EndpointsExtensions.MapCompositionHandlers), exception.Message);
        }

        [Fact]
        public async Task Works_together_with_the_global_switch()
        {
            var log = new List<string>();
            var client = CreateFactory(log, enableCompositionOverControllersGlobally: true).CreateClient();

            var response = await client.GetAsync("/api/composition-over-controllers-endpoint-filters/1");

            Assert.True(response.IsSuccessStatusCode);
            // Composed once, by the endpoint filter
            Assert.Equal(["action", "handler"], log);
        }

        [Fact]
        public void Fails_if_route_matching_settings_differ_from_the_global_switch()
        {
            var log = new List<string>();
            var factory = CreateFactory(log, enableCompositionOverControllersGlobally: true, useCaseInsensitiveRouteMatching: false);

            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("useCaseInsensitiveRouteMatching", exception.Message);
        }
    }
}
