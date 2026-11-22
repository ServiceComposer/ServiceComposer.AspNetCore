# Routing

Composition components declare the routes they contribute to with attributes, as MVC controller actions do.

## Attribute routing

Composition handlers, event subscribers, and endpoint scoped view model factories declare their routes with the ASP.NET Core HTTP method attributes, from the `Microsoft.AspNetCore.Mvc` namespace: `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpPatch]`, and `[HttpDelete]`. The attribute decorates the component's method, and its template uses the [ASP.NET Core route template syntax](https://learn.microsoft.com/aspnet/core/fundamentals/routing#route-templates), including route parameters and constraints:

<!-- snippet: routing-attribute -->
<a id='snippet-routing-attribute'></a>
```cs
public class ProductDetailsHandler : ICompositionRequestsHandler
{
    [HttpGet("/product/{id:int}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.ProductId = request.RouteValues["id"];
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/Routing/RoutingSnippets.cs#L10-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-routing-attribute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A method can have more than one attribute, to contribute to several routes:

<!-- snippet: routing-multiple-attributes -->
<a id='snippet-routing-multiple-attributes'></a>
```cs
public class ProductPriceHandler : ICompositionRequestsHandler
{
    [HttpGet("/product/{id:int}")]
    [HttpGet("/checkout/product/{id:int}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.ProductPrice = 100;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/Routing/RoutingSnippets.cs#L23-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-routing-multiple-attributes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The same attributes declare the routes of [contract-less composition handlers](contract-less-composition-requests-handlers.md).

### How components are composed

All the components declaring the same HTTP method and the same route template are composed into a single endpoint. Templates are compared as text, ignoring a leading `/`. In the previous snippets, both handlers contribute to `GET /product/{id:int}`, and `ProductPriceHandler` is also the only handler of `GET /checkout/product/{id:int}`.

> [!WARNING]
> Templates that match the same requests but are written differently, e.g., `/product/{id}` and `/product/{productId}`, or `/Product/{id}` and `/product/{id}`, aren't composed together. They become two endpoints matching the same requests, and the requests fail with an `AmbiguousMatchException`. Components meant to be composed together must use the same template.

`POST`, `PUT`, `PATCH`, and `DELETE` routes are mapped unless write support is disabled with `options.DisableWriteSupport()`.

> [!NOTE]
> The `[Route]` attribute on a component class doesn't prefix the templates of its methods: it's not supported ([#148](https://github.com/ServiceComposer/ServiceComposer.AspNetCore/issues/148)).

## Case sensitivity

Composition endpoints are regular ASP.NET Core endpoints, so requests are matched ignoring case, as for any other endpoint: a `GET /Product/1` request matches the `/product/{id:int}` template.

Composing components into endpoints is different: templates are compared case-sensitively, so `/Product/{id}` and `/product/{id}` aren't composed together (see the warning above).

### Composition over controllers

With [composition over controllers](composition-over-controllers.md), composition handlers' templates are matched to the controllers' route templates ignoring case by default. To match them case-sensitively, pass `useCaseInsensitiveRouteMatching: false` to `WithCompositionOverControllers()`:

<!-- snippet: routing-case-sensitive-composition-over-controllers -->
<a id='snippet-routing-case-sensitive-composition-over-controllers'></a>
```cs
app.MapControllers()
    .WithCompositionOverControllers(useCaseInsensitiveRouteMatching: false);
app.MapCompositionHandlers();
```
<sup><a href='/src/Snippets/Routing/RoutingSnippets.cs#L46-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-routing-case-sensitive-composition-over-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

All the controller endpoints must use the same setting. When composition over controllers is used, the same setting also applies to composing components into endpoints: by default, `/Product/{id}` and `/product/{id}` are composed together.
