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
<sup><a href='/src/Snippets/CompositionOverController.cs#L17-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-enable-composition-over-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If a route matches a controller endpoint and a set of composition handlers, ServiceComposer invokes the matching handlers after the controller action and before the view is rendered.

Composition over controllers supports both regular composition handlers (classes implementing `ICompositionRequestsHandler`) and [contract-less composition handlers](contract-less-composition-requests-handlers.md), including full model binding support for both.

Composition over controllers can be used as a templating engine leveraging the excellent Razor engine. Optionally, it can be used to add ViewModel Composition support to MVC web application without introducing a separate composition gateway.

`MapCompositionHandlers()` must be called too: it finds the composition handlers matching the routes of the controller endpoints. Composition handlers routes match controller routes ignoring case. To match them case-sensitively, call `WithCompositionOverControllers(useCaseInsensitiveRouteMatching: false)`. All controller endpoints must use the same setting.

## Controller action results

_Starting with v6.0.0_

The controller action runs first, then the composition handlers matching its route. What they compose into depends on the action result:

- If the action returns a model, the composition handlers compose into it: the controller acts as the [view model factory](view-model-factory.md) for the request.
- If the action returns no model, e.g. `View()` or `new ObjectResult(null)`, the composition handlers compose into the view model created by the view model factories, as for composition endpoints, and the action result gets it.
- If the action result can't carry a model, e.g. a redirect or `Ok()`, the composition handlers run, and the response is the controller's: the view model they compose into is discarded. Handlers with side effects, e.g. `POST` handlers composing over an action that redirects, run as for any other result.
- If the action result is an error, e.g. `NotFound(problemDetails)`, `Forbid()`, or any result with a 4xx or 5xx status code, the composition handlers don't run, and the response is the controller's.

Results that carry a model are `ViewResult`, `PartialViewResult`, and `ObjectResult` with no status code or a 2xx one, including models returned directly by the action. When the composition handlers run, an action result set by one of them with `SetActionResult` replaces the controller's.

In the following sample, the response contains both the name set by the controller and the price set by the composition handler:

<!-- snippet: composition-over-controllers-controller-model -->
<a id='snippet-composition-over-controllers-controller-model'></a>
```cs
[Route("/products")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        dynamic product = new ExpandoObject();
        product.Name = "Product name";

        // Composition handlers compose into the returned model
        return Ok(product);
    }
}

public class ProductPriceHandler : ICompositionRequestsHandler
{
    [HttpGet("/products/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.Price = 42m;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/CompositionOverController.cs#L46-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-over-controllers-controller-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Composition handlers writing the view model through `dynamic`, e.g. `vm.Price = 42m`, need a model that supports dynamic members, such as `ExpandoObject`. With a typed model, they fail at runtime if the model's type doesn't have those members: use `GetComposedResponseModel<T>()` instead.

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
<sup><a href='/src/Snippets/CompositionOverController.cs#L30-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-over-controllers-endpoint-filters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Composition runs as an endpoint filter, right after the controller action. Endpoint filters added before `WithCompositionOverControllers()` wrap both the action and the composition handlers, the same way they wrap composition handlers on endpoints mapped with `MapCompositionHandlers()`. For example, a filter that opens a transaction before calling `next` and commits after it covers the work done by the composition handlers.
- An action result set by a composition handler with `SetActionResult` replaces the controller action result, and outer endpoint filters see it.

Endpoint filters added later run inside composition, so `WithCompositionOverControllers()` must be the last endpoint filter added to the endpoints.

> [!NOTE]
> ASP.NET Core runs endpoint filters on controller endpoints inside MVC action filters. Composition handlers run before the `OnActionExecuted` part of action filters, and don't run if an action filter short-circuits the request, for example with the automatic HTTP 400 response of `[ApiController]` controllers.

## Model binding errors

The composition handlers' models declared with the `Bind*` attributes are bound before the controller action runs. If they have binding errors, the request gets a `400 Bad Request` and neither the action nor the composition handlers run. See [binding errors](model-binding.md#binding-errors).

## Authorization

_Available starting with v6.0.0_

Requests composing over a controller run on the controller endpoint. `WithCompositionOverControllers()` adds the metadata of the composition handlers, event subscribers, and view model factories composing over each controller endpoint to it, so their authorization attributes, such as `[Authorize]` or `[Authorize(Policy = "...")]`, are enforced before the controller action runs, together with the controller's and the action's. Callers must satisfy all of them.

As on composition endpoints, `[AllowAnonymous]` doesn't lift other requirements: if the controller, the action, or any component of the route requires authorization, `[AllowAnonymous]` is ignored for the route, also on the controller or the action, and a warning is logged when the endpoints are built. See [authentication and authorization](authentication-authorization.md).

Where ASP.NET Core uses a single metadata item of a type, the controller endpoint's own metadata wins over the components'. Conventions added after `WithCompositionOverControllers()` see the components' metadata, and a `CompositionEndpointComponents` item listing them. See [reading a route's components](endpoint-filters.md#reading-a-routes-components).

If the components' metadata can't be applied to the controller endpoint they compose over, e.g. because a convention removed it, the application fails at startup.
