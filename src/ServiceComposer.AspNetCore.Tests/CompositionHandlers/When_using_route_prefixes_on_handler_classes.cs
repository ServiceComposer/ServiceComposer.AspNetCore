using System;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlers
{
    public class When_using_route_prefixes_on_handler_classes
    {
        [Route("/api/products")]
        class PrefixedHandler : ICompositionRequestsHandler
        {
            [HttpGet("{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().Prefixed = true;
                return Task.CompletedTask;
            }
        }

        // Composed with PrefixedHandler: same combined template
        class AbsoluteHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/products/{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().Absolute = true;
                return Task.CompletedTask;
            }
        }

        [Route("api/ignored")]
        class AbsoluteTemplatesHandler : ICompositionRequestsHandler
        {
            [HttpGet("/absolute/{id}")]
            [HttpGet("~/tilde/{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().NotPrefixed = true;
                return Task.CompletedTask;
            }
        }

        [Route("/api/without-template")]
        class WithoutMethodTemplateHandler : ICompositionRequestsHandler
        {
            [HttpGet]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().WithoutTemplate = true;
                return Task.CompletedTask;
            }
        }

        [Route("/api/first")]
        [Route("/api/second/")]
        class MultiplePrefixesHandler : ICompositionRequestsHandler
        {
            [HttpGet("{id}")]
            [HttpPost("{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().MultiplePrefixes = true;
                return Task.CompletedTask;
            }
        }

        [CompositionHandler]
        [Route("/api/contract-less")]
        public class ContractlessHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("{id}")]
            public Task Get(int id)
            {
                httpContextAccessor.HttpContext!.Request.GetComposedResponseModel().ContractlessId = id;
                return Task.CompletedTask;
            }
        }

        [Route("/api/events")]
        class EventPublisher : ICompositionRequestsHandler
        {
            [HttpGet("{id}")]
            public Task Handle(HttpRequest request) => request.GetCompositionContext().RaiseEvent(new SomethingHappened());
        }

        record SomethingHappened;

        [Route("/api/events")]
        class PrefixedSubscriber : ICompositionEventsSubscriber
        {
            [HttpGet("{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<SomethingHappened>((_, request) =>
                {
                    request.GetComposedResponseModel().Subscribed = true;
                    return Task.CompletedTask;
                });
            }
        }

        static HttpClient CreateClient()
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<PrefixedHandler>();
                        options.RegisterCompositionHandler<AbsoluteHandler>();
                        options.RegisterCompositionHandler<AbsoluteTemplatesHandler>();
                        options.RegisterCompositionHandler<WithoutMethodTemplateHandler>();
                        options.RegisterCompositionHandler<MultiplePrefixesHandler>();
                        options.RegisterCompositionHandler<ContractlessHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_route_prefixes_on_handler_classes_ContractlessHandler_Get_int_id>();
                        options.RegisterCompositionHandler<EventPublisher>();
                        options.RegisterCompositionHandler<PrefixedSubscriber>();
                    });
                    services.AddRouting();
                    services.AddControllers();
                    services.AddHttpContextAccessor();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();
        }

        static async Task<JsonObject> Send(HttpMethod method, string path)
        {
            var response = await CreateClient().SendAsync(new HttpRequestMessage(method, path));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        }

        [Fact]
        public async Task The_class_route_prefixes_the_method_template()
        {
            var response = await Send(HttpMethod.Get, "/api/products/1");

            Assert.True(response["prefixed"]!.GetValue<bool>());
        }

        [Fact]
        public async Task Handlers_with_the_same_combined_template_are_composed_together()
        {
            var response = await Send(HttpMethod.Get, "/api/products/1");

            Assert.True(response["prefixed"]!.GetValue<bool>());
            Assert.True(response["absolute"]!.GetValue<bool>());
        }

        [Theory]
        [InlineData("/absolute/1")]
        [InlineData("/tilde/1")]
        public async Task Absolute_method_templates_are_not_prefixed(string path)
        {
            var response = await Send(HttpMethod.Get, path);

            Assert.True(response["notPrefixed"]!.GetValue<bool>());
            var notFound = await CreateClient().GetAsync("/api/ignored" + path);
            Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        }

        [Fact]
        public async Task A_method_attribute_without_template_uses_the_class_route()
        {
            var response = await Send(HttpMethod.Get, "/api/without-template");

            Assert.True(response["withoutTemplate"]!.GetValue<bool>());
        }

        [Theory]
        [InlineData("GET", "/api/first/1")]
        [InlineData("GET", "/api/second/1")]
        [InlineData("POST", "/api/first/1")]
        [InlineData("POST", "/api/second/1")]
        public async Task Each_class_route_prefixes_each_method_template(string method, string path)
        {
            var response = await Send(new HttpMethod(method), path);

            Assert.True(response["multiplePrefixes"]!.GetValue<bool>());
        }

        [Fact]
        public async Task Contract_less_handlers_are_prefixed()
        {
            var response = await Send(HttpMethod.Get, "/api/contract-less/42");

            Assert.Equal(42, response["contractlessId"]!.GetValue<int>());
        }

        [Fact]
        public async Task Event_subscribers_are_prefixed()
        {
            var response = await Send(HttpMethod.Get, "/api/events/1");

            Assert.True(response["subscribed"]!.GetValue<bool>());
        }
    }
}
