#nullable enable
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

#pragma warning disable SC0001

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_class_level_model_binding
    {
        public class Payload
        {
            public string? Text { get; set; }
        }

        public class SampleService
        {
            public string Value => "from-services";
        }

        [BindFromRoute<int>(routeValueKey: "id")]
        class RouteHandler : ICompositionRequestsHandler
        {
            [HttpGet("/class-route/{id}")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                request.GetComposedResponseModel().Value = arguments.Argument<int>("id", BindingSource.Path);
                return Task.CompletedTask;
            }
        }

        [BindFromQuery<string>(queryParameterName: "q")]
        class QueryHandler : ICompositionRequestsHandler
        {
            [HttpGet("/class-query")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                request.GetComposedResponseModel().Value = arguments.Argument<string>("q", BindingSource.Query);
                return Task.CompletedTask;
            }
        }

        [BindFromBody<Payload>]
        class BodyHandler : ICompositionRequestsHandler
        {
            [HttpPost("/class-body")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                request.GetComposedResponseModel().Value = arguments.Argument<Payload>(BindingSource.Body)!.Text;
                return Task.CompletedTask;
            }
        }

        [BindFromForm<string>(formFieldName: "name")]
        class FormHandler : ICompositionRequestsHandler
        {
            [HttpPost("/class-form")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                request.GetComposedResponseModel().Value = arguments.Argument<string>("name", BindingSource.Form);
                return Task.CompletedTask;
            }
        }

        [BindFromServices<SampleService>(parameterName: "service")]
        class ServicesHandler : ICompositionRequestsHandler
        {
            [HttpGet("/class-services")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                request.GetComposedResponseModel().Value = arguments.Argument<SampleService>("service", BindingSource.Services)!.Value;
                return Task.CompletedTask;
            }
        }

        [BindFromQuery<int>(queryParameterName: "page")]
        class ClassAndMethodHandler : ICompositionRequestsHandler
        {
            [HttpGet("/class-and-method/{id}")]
            [BindFromRoute<int>(routeValueKey: "id")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                var vm = request.GetComposedResponseModel();
                vm.Page = arguments.Argument<int>("page", BindingSource.Query);
                vm.Id = arguments.Argument<int>("id", BindingSource.Path);
                vm.Count = arguments!.Count;
                return Task.CompletedTask;
            }
        }

        // Same name and source on the class and on the method: the method's wins
        [BindFromRoute<int>(routeValueKey: "id")]
        class SameNameAndSourceHandler : ICompositionRequestsHandler
        {
            [HttpGet("/same-name-and-source/{id}")]
            [BindFromRoute<string>(routeValueKey: "id")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                var vm = request.GetComposedResponseModel();
                vm.Value = arguments.Argument<string>("id", BindingSource.Path);
                vm.Count = arguments!.Count;
                return Task.CompletedTask;
            }
        }

        // Both templates are composed into the same endpoint
        [BindFromRoute<int>(routeValueKey: "id")]
        class SameEndpointTwiceHandler : ICompositionRequestsHandler
        {
            [HttpGet("/same-endpoint-twice/{id}")]
            [HttpGet("same-endpoint-twice/{id}")]
            public Task Handle(HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                var vm = request.GetComposedResponseModel();
                vm.Value = arguments.Argument<int>("id", BindingSource.Path);
                vm.Count = arguments!.Count;
                return Task.CompletedTask;
            }
        }

        record SomethingHappened;

        class Publisher : ICompositionRequestsHandler
        {
            [HttpGet("/class-subscriber/{id}")]
            public Task Handle(HttpRequest request) => request.GetCompositionContext().RaiseEvent(new SomethingHappened());
        }

        [BindFromRoute<int>(routeValueKey: "id")]
        class Subscriber : ICompositionEventsSubscriber
        {
            [HttpGet("/class-subscriber/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<SomethingHappened>((_, request) =>
                {
                    var arguments = request.GetCompositionContext().GetArguments(this);
                    request.GetComposedResponseModel().SubscriberValue = arguments.Argument<int>("id", BindingSource.Path);
                    return Task.CompletedTask;
                });
            }
        }

        // Generic event handlers aren't bound to a route: nothing is bound for them
        [BindFromRoute<int>(routeValueKey: "id")]
        class GenericEventHandler : ICompositionEventsHandler<SomethingHappened>
        {
            public Task Handle(SomethingHappened @event, HttpRequest request)
            {
                var arguments = request.GetCompositionContext().GetArguments(this);
                request.GetComposedResponseModel().GenericHandlerHasArguments = arguments != null;
                return Task.CompletedTask;
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
                        options.RegisterCompositionHandler<RouteHandler>();
                        options.RegisterCompositionHandler<QueryHandler>();
                        options.RegisterCompositionHandler<BodyHandler>();
                        options.RegisterCompositionHandler<FormHandler>();
                        options.RegisterCompositionHandler<ServicesHandler>();
                        options.RegisterCompositionHandler<ClassAndMethodHandler>();
                        options.RegisterCompositionHandler<SameNameAndSourceHandler>();
                        options.RegisterCompositionHandler<SameEndpointTwiceHandler>();
                        options.RegisterCompositionHandler<Publisher>();
                        options.RegisterCompositionHandler<Subscriber>();
                        options.RegisterCompositionHandler<GenericEventHandler>();
                    });
                    services.AddRouting();
                    services.AddControllers();
                    services.AddSingleton<SampleService>();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();
        }

        static async Task<JsonObject> Read(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{(int)response.StatusCode}: {body}");
            return JsonNode.Parse(body)!.AsObject();
        }

        [Fact]
        public async Task Binds_from_the_route()
        {
            var response = await Read(await CreateClient().GetAsync("/class-route/42"));

            Assert.Equal(42, response["value"]!.GetValue<int>());
        }

        [Fact]
        public async Task Binds_from_the_query_string()
        {
            var response = await Read(await CreateClient().GetAsync("/class-query?q=sample"));

            Assert.Equal("sample", response["value"]!.GetValue<string>());
        }

        [Fact]
        public async Task Binds_from_the_body()
        {
            const string json = "{ \"text\": \"sample\" }";
            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            content.Headers.ContentLength = json.Length;

            var response = await Read(await CreateClient().PostAsync("/class-body", content));

            Assert.Equal("sample", response["value"]!.GetValue<string>());
        }

        [Fact]
        public async Task Binds_from_the_form()
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "sample" });

            var response = await Read(await CreateClient().PostAsync("/class-form", content));

            Assert.Equal("sample", response["value"]!.GetValue<string>());
        }

        [Fact]
        public async Task Binds_from_the_services()
        {
            var response = await Read(await CreateClient().GetAsync("/class-services"));

            Assert.Equal("from-services", response["value"]!.GetValue<string>());
        }

        [Fact]
        public async Task Class_and_method_attributes_both_bind()
        {
            var response = await Read(await CreateClient().GetAsync("/class-and-method/42?page=3"));

            Assert.Equal(3, response["page"]!.GetValue<int>());
            Assert.Equal(42, response["id"]!.GetValue<int>());
            Assert.Equal(2, response["count"]!.GetValue<int>());
        }

        [Fact]
        public async Task The_method_attribute_wins_for_the_same_name_and_source()
        {
            var response = await Read(await CreateClient().GetAsync("/same-name-and-source/42"));

            Assert.Equal("42", response["value"]!.GetValue<string>());
            Assert.Equal(1, response["count"]!.GetValue<int>());
        }

        [Fact]
        public async Task A_component_composed_twice_into_the_same_endpoint_binds_once()
        {
            var response = await Read(await CreateClient().GetAsync("/same-endpoint-twice/42"));

            Assert.Equal(42, response["value"]!.GetValue<int>());
            Assert.Equal(1, response["count"]!.GetValue<int>());
        }

        [Fact]
        public async Task Event_subscribers_get_their_class_level_arguments()
        {
            var response = await Read(await CreateClient().GetAsync("/class-subscriber/42"));

            Assert.Equal(42, response["subscriberValue"]!.GetValue<int>());
        }

        [Fact]
        public async Task Generic_event_handlers_get_no_arguments()
        {
            var response = await Read(await CreateClient().GetAsync("/class-subscriber/42"));

            Assert.False(response["genericHandlerHasArguments"]!.GetValue<bool>());
        }
    }
}
