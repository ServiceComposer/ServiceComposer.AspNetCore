# Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For example, it's possible to use the `Authorize` attribute to specify that a handler requires authorization. The authorization process is the regular ASP.NET Core 3.x process and no special configuration is needed to plugin ServiceComposer:

<!-- snippet: sample-handler-with-authorization -->
<a id='snippet-sample-handler-with-authorization'></a>
```cs
public class SampleHandlerWithAuthorization : ICompositionRequestsHandler
{
    [Authorize]
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/SampleHandler/SampleHandler.cs#L10-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-handler-with-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Attributes on the handler class apply too, as on a controller, also for contract-less composition handlers. `[AllowAnonymous]` on the method opts it out of `[Authorize]` on its class:

<!-- snippet: handler-class-with-authorization -->
<a id='snippet-handler-class-with-authorization'></a>
```cs
[Authorize]
public class OrdersHandler : ICompositionRequestsHandler
{
    [HttpGet("/orders/{id}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/SampleHandler/SampleHandler.cs#L22-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-handler-class-with-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When multiple handlers are registered for the same route, their authorization metadata is merged, and the most restrictive combination applies. `[AllowAnonymous]` on a handler doesn't lift the other handlers' requirements: if any handler of the route requires authorization, `[AllowAnonymous]` is ignored for that route. It only makes a route allow anonymous requests if no handler requires authorization, for example to opt the route out of an authorization fallback policy.
