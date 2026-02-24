using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class Post_with_2_handlers_1_subscriber_1_generic_subscriber
    {
        class TestEvent
        {
            public string AValue { get; set; }
        }

        class TestIntegerHandler : ICompositionRequestsHandler
        {
            class ANumberModel
            {
                public int ANumber { get; set; }
            }

            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var model = await request.Bind<BodyRequest<ANumberModel>>();
                vm.ANumber = model.Body.ANumber;
                
                var context = request.GetCompositionContext();
                await context.RaiseEvent(new TestEvent() {AValue = $"ANumber: {vm.ANumber}."});
            }
        }

        class TestStringHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.AString = content?.SelectToken("AString")?.Value<string>();
            }
        }

        class TestStringSubscriber : ICompositionEventsHandler<TestEvent>
        {
            public Task Handle(TestEvent @event, HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AValue = @event.AValue;

                return Task.CompletedTask;
            }
        }
        
        class TestIntegerSubscriber : ICompositionEventsSubscriber
        {
            [HttpPost("/sample/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<TestEvent>((@event, request) =>
                {
                    var vm = request.GetComposedResponseModel();
                    vm.AnIntegerValue = 32;

                    return Task.CompletedTask;
                });
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var expectedString = "this is a string value";
            var expectedNumber = 32;

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                        options.RegisterCompositionHandler<TestStringHandler>();
                        options.RegisterCompositionHandler<TestStringSubscriber>();
                        options.RegisterCompositionHandler<TestIntegerSubscriber>();
                    });
                    services.AddControllers();
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            dynamic model = new ExpandoObject();
            model.AString = expectedString;
            model.ANumber = expectedNumber;

            var json = (string) JsonConvert.SerializeObject(model);
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;

            // Act
            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            var response = await client.PostAsync("/sample/1", stringContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal(expectedString, responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(expectedNumber, responseObj?.SelectToken("ANumber")?.Value<int>());
            Assert.Equal(expectedNumber, responseObj?.SelectToken("AnIntegerValue")?.Value<int>());
            Assert.Equal($"ANumber: {expectedNumber}.", responseObj?.SelectToken("AValue")?.Value<string>());
        }
    }
}