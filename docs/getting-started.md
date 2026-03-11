# Getting started

## The problem

In a system built from multiple autonomous services, each service owns its own data. Displaying a product page, for example, might require:

- The **Sales** service to provide the price and availability
- The **Marketing** service to provide the name and description

The simplest approach is to call multiple service APIs from the front end and merge the results there. But this couples the client to service internals, leaks service boundaries, and causes chattiness. Another common reaction is to share a database or build a dedicated aggregation service — but both approaches erode service autonomy and lead to a distributed monolith.

**ViewModel Composition** is a technique that solves this at the API gateway layer. Each service contributes its own slice of data through a small, independent handler. The gateway runs all handlers in parallel and returns a single merged response to the client. No service knows about the others, and the client makes a single request.

ServiceComposer is an ASP.NET Core implementation of this pattern.

## Prerequisites

- .NET 10 or later
- An ASP.NET Core web application project acting as the API gateway

## Installation

Add the NuGet package to the gateway project:

```
dotnet add package ServiceComposer.AspNetCore
```

## Setting up the gateway

Configure ServiceComposer in `Program.cs`:

<!-- snippet: sample-startup -->
<a id='snippet-sample-startup'></a>
```cs
var builder = WebApplication.CreateBuilder();
builder.Services.AddRouting();
builder.Services.AddViewModelComposition();

var app = builder.Build();
app.MapCompositionHandlers();
app.Run();
```
<sup><a href='/src/Snippets/BasicUsage/Startup.cs#L11-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

That is all the gateway project needs. ServiceComposer scans loaded assemblies at startup and automatically discovers composition handlers.

## Writing composition handlers

Create a class library project for each service's composition handlers (e.g. `Sales.ViewModelComposition`, `Marketing.ViewModelComposition`). Add a package reference to `ServiceComposer.AspNetCore` in each, then define a handler class.

A handler implements `ICompositionRequestsHandler` and is decorated with an `[Http*]` routing attribute matching the route it contributes to. Multiple handlers from different assemblies can be registered for the same route — they run in parallel and each writes its own properties onto the shared view model.

**Sales handler** — contributes price data:

<!-- snippet: basic-usage-sales-handler -->
<a id='snippet-basic-usage-sales-handler'></a>
```cs
public class SalesProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();

        //retrieve product details from the sales database or service
        vm.ProductId = request.HttpContext.GetRouteValue("id").ToString();
        vm.ProductPrice = 100;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/BasicUsage/SalesProductInfo.cs#L9-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-basic-usage-sales-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Marketing handler** — contributes name and description:

<!-- snippet: basic-usage-marketing-handler -->
<a id='snippet-basic-usage-marketing-handler'></a>
```cs
public class MarketingProductInfo: ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();

        //retrieve product details from the marketing database or service
        vm.ProductName = "Sample product";
        vm.ProductDescription = "This is a sample product";
        
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/BasicUsage/MarketingProductInfo.cs#L8-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-basic-usage-marketing-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Both handlers target the same route (`/product/{id}`), but they are completely independent: neither knows about the other, and they are free to evolve separately.

> [!NOTE]
> Each service should own distinct, non-overlapping properties on the view model. Writing to the same property from two handlers running in parallel produces a data race. See [thread safety](thread-safety.md) for details.

## Making the gateway aware of your handlers

Reference each handler class library from the gateway project. ServiceComposer's assembly scanner picks up any `ICompositionRequestsHandler` implementation in a loaded assembly automatically.

```xml
<!-- In the gateway .csproj -->
<ItemGroup>
  <ProjectReference Include="..\Sales.ViewModelComposition\Sales.ViewModelComposition.csproj" />
  <ProjectReference Include="..\Marketing.ViewModelComposition\Marketing.ViewModelComposition.csproj" />
</ItemGroup>
```

In production, handlers are typically packaged as separate NuGet packages and referenced that way rather than via project references.

## Trying it out

Run the gateway and issue a GET request to `/product/1`. The response is a single merged JSON object:

```json
{
  "productId": "1",
  "productPrice": 100,
  "productName": "Sample product",
  "productDescription": "This is a sample product"
}
```

No client-side merging, no shared database, no knowledge between services.

## How it works

When a request arrives at a composition endpoint, ServiceComposer:

1. Resolves all handlers registered for that route
2. Runs all `Handle()` methods **in parallel** via `Task.WhenAll`
3. Each handler reads from the request and writes to the shared view model
4. The composed view model is serialized and returned to the caller

Adding a new service means adding a new handler class library. Existing handlers require no changes.

## Next steps

| Topic | When you need it |
|---|---|
| [Strongly typed view models](view-model-factory.md) | Replace `dynamic` with a concrete class |
| [Events](events.md) | Coordinate between handlers within a request (e.g. composing lists) |
| [Model binding](model-binding.md) | Bind request body, route values, and query strings in handlers |
| [Contract-less handlers](contract-less-composition-requests-handlers.md) | Controller-action syntax without implementing an interface |
| [Authentication and authorization](authentication-authorization.md) | Secure composition routes |
| [Thread safety](thread-safety.md) | Handle shared dependencies safely |
| [Scatter/Gather](scatter-gather.md) | Fan out HTTP requests to downstream services |
