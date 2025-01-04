# Events

When handling composition requests, there are scenarios in which request handlers need to offload some of the composition concerns to other handlers.

> [!NOTE]
> Composing lists of composed elements or master-details type of outputs is one scenario where events are needed. For an introduction to composing lists and the related challenges, read the [Into the darkness of ViewModels Lists Composition](https://milestone.topics.it/2019/02/28/into-the-darkness-of-viewmodel-lists-composition.html) blog post.

Events are regular .NET types, classes, or records, like in the following example:

<!-- snippet: an-event -->
<a id='snippet-an-event'></a>
```cs
public record AnEvent(string SomeValue);
```
<sup><a href='/src/Snippets/Events/AnEvent.cs#L3-L5' title='Snippet source file'>snippet source</a> | <a href='#snippet-an-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Events are synchronous, and in memory, they don't need to be serializable.

## Publishing events

Publishing an event is done through the composition context, as demonstrated by the following snippet:

<!-- snippet: publishing-events -->
<a id='snippet-publishing-events'></a>
```cs
public class EventPublishingHandler : ICompositionRequestsHandler
{
    [HttpGet("/route-based-handler/{some-id}")]
    public async Task Handle(HttpRequest request)
    {
        var context = request.GetCompositionContext();
        await context.RaiseEvent(new AnEvent(SomeValue: "This is the value"));
    }
}
```
<sup><a href='/src/Snippets/Events/EventPublishingHandler.cs#L8-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-publishing-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Subscribing to events

ServiceComposer offers two APIs to subscribe to events.

### Generic event handlers

Subscribing to events can be done by creating a class that implements the `ICompositionEventsHandler<TEvent>` interface:

<!-- snippet: generic-event-handler -->
<a id='snippet-generic-event-handler'></a>
```cs
public class GenericEventHandler : ICompositionEventsHandler<AnEvent>
{
    public Task Handle(AnEvent @event, HttpRequest request)
    {
        // handle the event
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/Events/GenericEventHandler.cs#L7-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-generic-event-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICompositionEventsHandler<TEvent>` instances are discovered at assembly scanning time and registered in the DI container as transient components. Hence, event handlers support dependency injection.

### Route-based subscribers

A more granular way to subscribe to events is by creating a class that implements the `ICompositionEventsSubscriber`:

<!-- snippet: route-based-event-handler -->
<a id='snippet-route-based-event-handler'></a>
```cs
public class RouteBasedEventHandler : ICompositionEventsSubscriber
{
    [HttpGet("/route-based-handler/{some-id}")]
    public void Subscribe(ICompositionEventsPublisher publisher)
    {
        publisher.Subscribe<AnEvent>((@event, request) =>
        {
            // handle the event
            return Task.CompletedTask;
        });
    }
}
```
<sup><a href='/src/Snippets/Events/RouteBasedEventHandler.cs#L8-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-route-based-event-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> [!NOTE]
> The class must also be decorated with one or more route attributes. Otherwise, it'll never be invoked.

At runtime, when an HTTP request that matches the route pattern is handled, all matching subscribers will be invoked, giving them the opportunity to subscribe. The registered event handler will be invoked when a publisher publishes the subscribed event.

## When using which

Generic event handlers, classes implementing `ICompositionEventsHandler<TEvent>`, are invoked every time an event they subscribe to is published, regardless of the currently handled route. If the same event is used in multiple scenarios, e.g., when doing an HTTP GET and a POST, and different behaviors are required, it's better to implement a route-based event handler that can easily, through the route attribute, differentiate to which type of request it reacts to.
