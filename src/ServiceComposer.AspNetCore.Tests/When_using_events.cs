using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Xunit;
using ServiceComposer.AspNetCore.Testing;
using Newtonsoft.Json.Linq;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_events
    {
        class AHandler : IHandleRequests
        {
            public async Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                var id = int.Parse((string)routeData.Values["id"]);
                vm.HandlerValue = id;

                await vm.RaiseEvent(new AnEvent());
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return true;
            }
        }

        class ASubscriber : ISubscribeToCompositionEvents
        {
            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return true;
            }

            public void Subscribe(IPublishCompositionEvents publisher)
            {
                publisher.Subscribe<AnEvent>((requestId, viewModel, @event, routeData, httpRequest) => 
                {
                    var id = int.Parse((string)routeData.Values["id"]);
                    viewModel.SubscriberValue = id;

                    return Task.CompletedTask;
                });
            }
        }

        public class AnEvent
        {

        }

        [Fact]
        public async Task Should_invoke_subscribers()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterRequestsHandler<AHandler>();
                        options.RegisterCompositionEventsSubscriber<ASubscriber>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/matching-handlers/32");

            // Assert
            response.EnsureSuccessStatusCode();

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(32, (int)body.handlerValue);
            Assert.Equal(32, (int)body.subscriberValue);
        }
    }
}
