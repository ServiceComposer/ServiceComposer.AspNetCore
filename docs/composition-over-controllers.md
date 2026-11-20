# Composition over controllers

ServiceComposer can be used to enhance a MVC web application by adding composition support to Controllers. ServiceComposer can be configured to use a technique called "Composition over controllers":

<!-- snippet: enable-composition-over-controllers -->
<a id='snippet-enable-composition-over-controllers'></a>
```cs
builder.Services.AddViewModelComposition(options =>
{
    options.EnableCompositionOverControllers();
});
```
<sup><a href='/src/Snippets/CompositionOverController.cs#L13-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-enable-composition-over-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Once composition over controllers is enabled, ServiceComposer will inject a MVC filter to intercept all controllers invocations. If a route matches a regular controller and a set of composition handlers ServiceComposer will invoke the matching handlers after the controller and before the view is rendered.

Composition over controllers supports both regular composition handlers (classes implementing `ICompositionRequestsHandler`) and [contract-less composition handlers](contract-less-composition-requests-handlers.md), including full model binding support for both.

Composition over controllers can be used as a templating engine leveraging the excellent Razor engine. Optionally, it can be used to add ViewModel Composition support to MVC web application without introducing a separate composition gateway.

## Composition and endpoint filters

By default, composition handlers run in an MVC result filter, after the controller action returned. [Endpoint filters](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters) added to controller endpoints wrap only the action, and run before the composition handlers.

To run composition inside the endpoint filter pipeline, call `WithCompositionOverControllers()` on the controller endpoints:

<!-- snippet: composition-over-controllers-endpoint-filters -->
<a id='snippet-composition-over-controllers-endpoint-filters'></a>
```cs
app.MapControllers()
    // Wraps both the controller action and the composition handlers
    .AddEndpointFilter(async (context, next) =>
    {
        var result = await next(context);
        // result is the action result, including the one
        // set by composition handlers with SetActionResult
        return result;
    })
    // Must be the last endpoint filter added
    .WithCompositionOverControllers();
```
<sup><a href='/src/Snippets/CompositionOverController.cs#L23-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-over-controllers-endpoint-filters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

With `WithCompositionOverControllers()`:

- Composition runs as an endpoint filter, right after the controller action. Endpoint filters added before `WithCompositionOverControllers()` wrap both the action and the composition handlers, the same way they wrap composition handlers on endpoints mapped with `MapCompositionHandlers()`. For example, a filter that opens a transaction before calling `next` and commits after it covers the work done by the composition handlers.
- An action result set by a composition handler with `SetActionResult` replaces the controller action result, and outer endpoint filters see it. With the default result filter, action results set by composition handlers are ignored.

Endpoint filters added later run inside composition, so `WithCompositionOverControllers()` must be the last endpoint filter added to the endpoints.

> [!NOTE]
> ASP.NET Core runs endpoint filters on controller endpoints inside MVC action filters. With `WithCompositionOverControllers()`, composition handlers run before the `OnActionExecuted` part of action filters, and don't run if an action filter short-circuits the request, for example with the automatic HTTP 400 response of `[ApiController]` controllers.
