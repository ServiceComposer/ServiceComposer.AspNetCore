using System;
using System.Collections.Concurrent;
using System.Linq;
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
    namespace Controllers
    {
        [Route("/api/composition-filters")]
        public class CompositionFiltersController : ControllerBase
        {
            [HttpGet("{id}")]
            public Task<object> Get(int id)
            {
                return Task.FromResult((object)null);
            }
        }
    }

    public class When_using_composition_filters
    {
        class InvocationsLog
        {
            public ConcurrentQueue<string> Entries { get; } = new();

            public static void Add(HttpContext context, string entry)
            {
                context.RequestServices.GetRequiredService<InvocationsLog>().Entries.Enqueue(entry);
            }
        }

        class RecordingFilterAttribute(string name) : CompositionRequestFilterAttribute
        {
            public override async ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
            {
                InvocationsLog.Add(context.HttpContext, $"{name}:{context.Handler.GetType().Name}");
                await next(context);
            }
        }

        class SkippingFilterAttribute : CompositionRequestFilterAttribute
        {
            public override ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
            {
                return ValueTask.CompletedTask;
            }
        }

        [RecordingFilter("class")]
        class FilteredHandler : ICompositionRequestsHandler
        {
            [HttpGet("/filters/{id}"), RecordingFilter("method")]
            public Task Handle(HttpRequest request)
            {
                InvocationsLog.Add(request.HttpContext, nameof(FilteredHandler));
                return Task.CompletedTask;
            }
        }

        class NotFilteredHandler : ICompositionRequestsHandler
        {
            [HttpGet("/filters/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.NotFilteredHandlerRan = true;
                return Task.CompletedTask;
            }
        }

        class TypedFilter : ICompositionRequestFilter<FilteredHandler>
        {
            public async ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
            {
                InvocationsLog.Add(context.HttpContext, $"typed:{context.Handler.GetType().Name}");
                await next(context);
            }
        }

        class AnotherTypedFilter : ICompositionRequestFilter<FilteredHandler>
        {
            public async ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
            {
                InvocationsLog.Add(context.HttpContext, $"another-typed:{context.Handler.GetType().Name}");
                await next(context);
            }
        }

        class SkippedHandler : ICompositionRequestsHandler
        {
            [HttpGet("/skipping-filters/{id}"), SkippingFilter]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.SkippedHandlerRan = true;
                return Task.CompletedTask;
            }
        }

        class NotSkippedHandler : ICompositionRequestsHandler
        {
            [HttpGet("/skipping-filters/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.NotSkippedHandlerRan = true;
                return Task.CompletedTask;
            }
        }

        class HandlerOverController : ICompositionRequestsHandler
        {
            [HttpGet("/api/composition-filters/{id}"), RecordingFilter("over-controller")]
            public Task Handle(HttpRequest request)
            {
                InvocationsLog.Add(request.HttpContext, nameof(HandlerOverController));
                var vm = request.GetComposedResponseModel();
                vm.HandlerOverControllerRan = true;
                return Task.CompletedTask;
            }
        }

        static (System.Net.Http.HttpClient Client, InvocationsLog Log) CreateClient(Action<ViewModelCompositionOptions> configure, bool overControllers = false)
        {
            var log = new InvocationsLog();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_composition_filters>
            (
                configureServices: services =>
                {
                    services.AddSingleton(log);
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        configure(options);
                    });
                    services.AddRouting();
                    if (overControllers)
                    {
                        services.AddControllers().AddNewtonsoftJson();
                    }
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        if (overControllers)
                        {
                            builder.MapControllers().WithCompositionOverControllers();
                        }
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

            return (client, log);
        }

        [Fact]
        public async Task Filters_run_right_before_their_handler_only()
        {
            var (client, log) = CreateClient(options =>
            {
                options.RegisterCompositionHandler<FilteredHandler>();
                options.RegisterCompositionHandler<NotFilteredHandler>();
                options.RegisterCompositionRequestsFilter(typeof(TypedFilter));
                options.RegisterCompositionRequestsFilter(typeof(AnotherTypedFilter));
            });

            var response = await client.GetAsync("/filters/1");

            Assert.True(response.IsSuccessStatusCode);
            dynamic body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.True((bool)body.NotFilteredHandlerRan);
            Assert.Equal(
            [
                "class:FilteredHandler",
                "method:FilteredHandler",
                "typed:FilteredHandler",
                "another-typed:FilteredHandler",
                "FilteredHandler"
            ], log.Entries.ToArray());
        }

        [Fact]
        public async Task A_filter_not_calling_next_skips_its_handler_only()
        {
            var (client, _) = CreateClient(options =>
            {
                options.RegisterCompositionHandler<SkippedHandler>();
                options.RegisterCompositionHandler<NotSkippedHandler>();
            });

            var response = await client.GetAsync("/skipping-filters/1");

            Assert.True(response.IsSuccessStatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.Value<bool>("NotSkippedHandlerRan"));
            Assert.Null(body["SkippedHandlerRan"]);
        }

        [Fact]
        public async Task Filters_run_with_composition_over_controllers()
        {
            var (client, log) = CreateClient(options =>
            {
                options.RegisterCompositionHandler<HandlerOverController>();
            }, overControllers: true);

            var response = await client.GetAsync("/api/composition-filters/1");

            Assert.True(response.IsSuccessStatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.Value<bool>("HandlerOverControllerRan"));
            Assert.Equal(["over-controller:HandlerOverController", nameof(HandlerOverController)], log.Entries.ToArray());
        }
    }
}
