---
title: ServiceComposer.AspNetCore — key interfaces
tags:
  - servicecomposer
  - interfaces
  - contracts
  - api
lifecycle: permanent
createdAt: '2026-03-12T19:42:19.191Z'
updatedAt: '2026-03-12T19:42:19.191Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
## Request Handling Interfaces

| Interface | Purpose |
| --- | --- |
| `ICompositionRequestsHandler` | Core handler: `Task Handle(HttpRequest request)` — contributes data to the view model |
| `ICompositionEventsSubscriber` | Route-scoped event subscriber: `void Subscribe(ICompositionEventsPublisher)` |
| `ICompositionEventsHandler<T>` | Global event handler: `Task Handle(T @event, HttpRequest)` — handles events from any route |
| `ICompositionEventsPublisher` | Event bus within a single request: `void Subscribe<T>(CompositionEventHandler<T>)` |
| `ICompositionContext` | Request context: provides `RequestId`, `RaiseEvent<T>()`, and `GetArguments()` for model binding |

## View Model Creation Interfaces

| Interface | Purpose |
| --- | --- |
| `IViewModelFactory` | Creates the view model object for a request (default: `ExpandoObject`) |
| `IEndpointScopedViewModelFactory` | Marker extending `IViewModelFactory` — one per endpoint, overrides global factory |
| `IViewModelPreviewHandler` | Visitor pattern: `Task Preview(HttpRequest)` — inspect/modify the view model before handlers run |

## Filtering Interfaces

| Interface | Purpose |
| --- | --- |
| `ICompositionRequestFilter` | Filter wrapping composition handler execution |
| `ICompositionRequestFilter<T>` | Type-specific filter for a particular handler type |
| `CompositionRequestFilterAttribute` | Attribute-based filter applied to handler methods |
| ASP.NET Core `IEndpointFilter` | Standard endpoint filters, applied via `MapCompositionHandlers().AddEndpointFilter()` |

## Error Handling and Extensibility

| Interface | Purpose |
| --- | --- |
| `ICompositionErrorsHandler` | `Task OnRequestError(HttpRequest, Exception)` — invoked when a handler throws |
| `IViewModelCompositionOptionsCustomization` | Plugin interface for external assemblies to customize options during scanning |

## Key Extension Methods

```csharp
// On IServiceCollection
services.AddViewModelComposition();
services.AddViewModelComposition(Action<ViewModelCompositionOptions>);
services.AddViewModelComposition(IConfiguration);

// On IEndpointRouteBuilder
builder.MapCompositionHandlers() -> IEndpointConventionBuilder

// On HttpRequest
request.GetComposedResponseModel() -> dynamic
request.GetComposedResponseModel<T>() -> T
request.GetCompositionContext() -> ICompositionContext
request.SetActionResult(ActionResult)
request.Bind<T>() -> T
request.TryBind<T>() -> (T, bool, ModelStateDictionary)
```
