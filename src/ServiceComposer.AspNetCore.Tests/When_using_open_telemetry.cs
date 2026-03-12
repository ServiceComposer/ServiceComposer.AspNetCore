using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_open_telemetry
    {
        static ActivityListener CreateActivityListener(ConcurrentBag<Activity> capturedActivities)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "ServiceComposer.AspNetCore.ViewModelComposition",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => capturedActivities.Add(activity)
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }

        // Each test uses its own uniquely named handler types to avoid
        // interference from parallel tests that share the same ActivityListener source.

        class SingleTestHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Handler_span_is_created_with_correct_name_and_tags()
        {
            // Arrange
            var capturedActivities = new ConcurrentBag<Activity>();
            using var listener = CreateActivityListener(capturedActivities);

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<SingleTestHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var handlerType = typeof(SingleTestHandler);
            var handlerActivity = Assert.Single(capturedActivities, a => a.DisplayName == $"composition.handler {handlerType.FullName}");
            Assert.Equal(handlerType.FullName, handlerActivity.GetTagItem("composition.handler.type"));
            Assert.Equal(handlerType.Namespace, handlerActivity.GetTagItem("composition.handler.namespace"));
        }

        class FirstOfTwoTestHandlers : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().AString = "sample";
                return Task.CompletedTask;
            }
        }

        class SecondOfTwoTestHandlers : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().ANumber = 42;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task One_handler_span_per_handler_is_created()
        {
            // Arrange
            var capturedActivities = new ConcurrentBag<Activity>();
            using var listener = CreateActivityListener(capturedActivities);

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<FirstOfTwoTestHandlers>();
                        options.RegisterCompositionHandler<SecondOfTwoTestHandlers>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.Single(capturedActivities, a => a.DisplayName == $"composition.handler {typeof(FirstOfTwoTestHandlers).FullName}");
            Assert.Single(capturedActivities, a => a.DisplayName == $"composition.handler {typeof(SecondOfTwoTestHandlers).FullName}");
        }

        class TestEventRaisedByHandler { }

        class HandlerThatRaisesTestEvent : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var context = request.GetCompositionContext();
                await context.RaiseEvent(new TestEventRaisedByHandler());
            }
        }

        class SubscriberForTestEvent : ICompositionEventsSubscriber
        {
            [HttpGet("/sample/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<TestEventRaisedByHandler>((@event, request) => Task.CompletedTask);
            }
        }

        [Fact]
        public async Task Event_span_is_created_as_child_of_handler_span()
        {
            // Arrange
            var capturedActivities = new ConcurrentBag<Activity>();
            using var listener = CreateActivityListener(capturedActivities);

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<HandlerThatRaisesTestEvent>();
                        options.RegisterCompositionHandler<SubscriberForTestEvent>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var handlerType = typeof(HandlerThatRaisesTestEvent);
            var eventType = typeof(TestEventRaisedByHandler);
            var handlerActivity = Assert.Single(capturedActivities, a => a.DisplayName == $"composition.handler {handlerType.FullName}");
            var eventActivity = Assert.Single(capturedActivities, a => a.DisplayName == $"composition.event {eventType.FullName}");
            Assert.Equal(handlerActivity.Id, eventActivity.ParentId);
        }

        class HandlerThatThrowsForOTelTest : ICompositionRequestsHandler
        {
            [HttpGet("/failing/{id}")]
            public Task Handle(HttpRequest request)
            {
                throw new InvalidOperationException("Something went wrong");
            }
        }

        [Fact]
        public async Task Handler_span_has_error_status_when_handler_throws()
        {
            // Arrange
            var capturedActivities = new ConcurrentBag<Activity>();
            using var listener = CreateActivityListener(capturedActivities);

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<HandlerThatThrowsForOTelTest>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/failing/1"));

            // Assert
            var handlerType = typeof(HandlerThatThrowsForOTelTest);
            var handlerActivity = Assert.Single(capturedActivities, a => a.DisplayName == $"composition.handler {handlerType.FullName}");
            Assert.Equal(ActivityStatusCode.Error, handlerActivity.Status);
            Assert.Equal("Something went wrong", handlerActivity.StatusDescription);
        }
    }
}
