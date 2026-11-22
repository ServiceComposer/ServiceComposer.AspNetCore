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

ServiceComposer offers two APIs to subscribe to events: generic event handlers, the recommended default, and route-based subscribers, for when the handling depends on the route. See [when using which](#when-using-which).

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

`ICompositionEventsHandler<TEvent>` instances are discovered at assembly scanning time and registered in the DI container as transient components. Hence, event handlers support constructor dependency injection, and are plain classes that are easy to unit test.

A generic event handler is invoked every time the event is published, whatever the route of the request: the event type is the contract between the publisher and the handlers.

### Route-based subscribers

When the same event needs different handling depending on the route, subscribe to it by creating a class that implements the `ICompositionEventsSubscriber`:

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
<sup><a href='/src/Snippets/Events/RouteBasedEventHandler.cs#L7-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-route-based-event-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> [!NOTE]
> The class must also be decorated with one or more route attributes. Otherwise, it'll never be invoked.

At runtime, when an HTTP request that matches the route pattern is handled, all matching subscribers will be invoked, giving them the opportunity to subscribe. The registered event handler will be invoked when a publisher publishes the subscribed event.

## When using which

Use generic event handlers, classes implementing `ICompositionEventsHandler<TEvent>`, by default:

- **The event type is the only contract.** A generic handler is invoked whenever the event is published. If the route of the publishing handler changes, or another route starts publishing the same event, it keeps working.
- **Less ceremony.** A `Handle` method with constructor injection, instead of a `Subscribe` method, a lambda, and route attributes. It's also easier to unit test.
- **Familiar.** It's the same shape as other .NET handler interfaces, such as NServiceBus `IHandleMessages<T>` or MediatR notification handlers.

Use route-based subscribers, classes implementing `ICompositionEventsSubscriber`, when the same event is published on several routes and needs different handling on each of them, e.g., an HTTP GET and a POST. Their route attributes make that explicit. Before using them, consider whether two different events express the difference more clearly.

> [!WARNING]
> Route-based subscribers repeat the route template of the publishing handler. If it changes, or another route publishes the event, they silently stop being invoked: nothing fails.
