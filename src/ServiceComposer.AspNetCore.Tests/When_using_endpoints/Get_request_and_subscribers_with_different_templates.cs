using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.When_using_endpoints
{
    public class Get_request_and_subscribers_with_different_templates
    {
        class TestEvent{}

        class TestGetHandlerThatAppendAStringAndRaisesTestEvent : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var vm = request.GetResponseModel();
                vm.AString = "sample";

                await vm.RaiseEvent(new TestEvent());
            }
        }

        class TestGetSubscriberNotUsedTemplate : ICompositionEventsSubscriber
        {
            [HttpGet("/this-is-never-used")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<TestEvent>((@event, request) =>
                {
                    var vm = request.GetResponseModel();
                    vm.ThisShouldNeverBeAppended = "sample";
                    return Task.CompletedTask;
                });
            }
        }

        class TestGetSubscriberThatAppendAnotherStringWhenTestEventIsRaised : ICompositionEventsSubscriber
        {
            [HttpGet("/sample/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<TestEvent>((@event, request) =>
                {
                    var vm = request.GetResponseModel();
                    vm.AnotherString = "sample";
                    return Task.CompletedTask;
                });
            }
        }

        [Fact]
        public async Task Invokes_only_subscribers_with_the_expected_template()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandlerThatAppendAStringAndRaisesTestEvent>();
                        options.RegisterCompositionHandler<TestGetSubscriberThatAppendAnotherStringWhenTestEventIsRaised>();
                        options.RegisterCompositionHandler<TestGetSubscriberNotUsedTemplate>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal("sample", responseObj?.SelectToken("AnotherString")?.Value<string>());
            Assert.False(responseObj.ContainsKey("ThisShouldNeverBeAppended"));
        }
    }
}