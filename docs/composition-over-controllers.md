# Composition over controllers

ServiceComposer can be used to enhance a MVC web application by adding composition support to Controllers. To use a technique called "Composition over controllers", call `WithCompositionOverControllers()` on the controller endpoints:

<!-- snippet: enable-composition-over-controllers -->
<a id='snippet-enable-composition-over-controllers'></a>
```cs
builder.Services.AddViewModelComposition();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers()
    .WithCompositionOverControllers();
app.MapCompositionHandlers();
```
<sup><a href='/src/Snippets/CompositionOverController.cs#L14-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-enable-composition-over-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If a route matches a controller endpoint and a set of composition handlers, ServiceComposer invokes the matching handlers after the controller action and before the view is rendered.

Composition over controllers supports both regular composition handlers (classes implementing `ICompositionRequestsHandler`) and [contract-less composition handlers](contract-less-composition-requests-handlers.md), including full model binding support for both.

Composition over controllers can be used as a templating engine leveraging the excellent Razor engine. Optionally, it can be used to add ViewModel Composition support to MVC web application without introducing a separate composition gateway.

`MapCompositionHandlers()` must be called too: it finds the composition handlers matching the routes of the controller endpoints. Composition handlers routes match controller routes ignoring case. To match them case-sensitively, call `WithCompositionOverControllers(useCaseInsensitiveRouteMatching: false)`. All controller endpoints must use the same setting.

## Composition and endpoint filters

Composition handlers run as an [endpoint filter](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters). Endpoint filters added to the controller endpoints before calling `WithCompositionOverControllers()` wrap both the controller action and the composition handlers:

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
<sup><a href='/src/Snippets/CompositionOverController.cs#L27-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-over-controllers-endpoint-filters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Composition runs as an endpoint filter, right after the controller action. Endpoint filters added before `WithCompositionOverControllers()` wrap both the action and the composition handlers, the same way they wrap composition handlers on endpoints mapped with `MapCompositionHandlers()`. For example, a filter that opens a transaction before calling `next` and commits after it covers the work done by the composition handlers.
- An action result set by a composition handler with `SetActionResult` replaces the controller action result, and outer endpoint filters see it.

Endpoint filters added later run inside composition, so `WithCompositionOverControllers()` must be the last endpoint filter added to the endpoints.

> [!NOTE]
> ASP.NET Core runs endpoint filters on controller endpoints inside MVC action filters. Composition handlers run before the `OnActionExecuted` part of action filters, and don't run if an action filter short-circuits the request, for example with the automatic HTTP 400 response of `[ApiController]` controllers.

## Authorization

On routes that compose over a controller, authorization metadata declared on composition handlers, such as `[Authorize]` or `[Authorize(Policy = "...")]`, is ignored. The request runs on the controller endpoint, and ASP.NET Core only evaluates the controller endpoint's authorization metadata, before the controller action and the composition handlers run.

Declare authorization on the controller or the action, or call `RequireAuthorization()` on the controller endpoints. It covers the composition handlers too. See [#1119](https://github.com/ServiceComposer/ServiceComposer.AspNetCore/issues/1119).

## EnableCompositionOverControllers()

`EnableCompositionOverControllers()` is obsolete, throws a `NotSupportedException`, and will be removed in version 7.0.0. It enabled composition over controllers for all controller endpoints, running composition handlers in an MVC result filter. See the [upgrade guide](upgrade-guides/5.x.x-6.0.0.md).
