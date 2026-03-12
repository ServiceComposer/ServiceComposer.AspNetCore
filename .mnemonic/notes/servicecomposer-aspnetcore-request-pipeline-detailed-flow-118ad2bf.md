---
title: ServiceComposer.AspNetCore — request pipeline detailed flow
tags:
  - servicecomposer
  - pipeline
  - request-flow
  - composition
  - controllers
lifecycle: permanent
createdAt: '2026-03-12T19:42:35.864Z'
updatedAt: '2026-03-12T19:42:35.864Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
## 1. Startup Registration

```text
services.AddViewModelComposition() -> ViewModelCompositionOptions
  - Assembly scanning discovers types implementing key interfaces
  - Registers them as transient services in DI
  - Populates CompositionMetadataRegistry with component types and event handler mappings
```

## 2. Endpoint Mapping

```text
endpoints.MapCompositionHandlers()
  - Groups all registered components by route template (from [HttpGet], [HttpPost], etc.)
  - For each unique route+method combination, creates a CompositionEndpointBuilder
  - If CompositionOverControllers is enabled AND a controller already owns the route,
    stores those handler types and their method metadata in CompositionOverControllersRoutes instead
  - Registers endpoints via CompositionEndpointDataSource
```

## 3. Request Flow at a Composition Endpoint

```text
HTTP Request
  |
  v
Enable request body buffering (allows multiple handlers to read body)
  |
  v
Model Binding Phase (ComponentsModelBinder)
  - For each component with [BindModel*] attributes on its Handle/Subscribe method
  - Uses RequestModelBinder (wraps ASP.NET Core's IModelBinderFactory)
  - Results stored in IDictionary<Type, IList<ModelBindingArgument>>
  |
  v
Endpoint Filter Pipeline (IEndpointFilter chain, cached after first build)
  |
  v
Composition Request Filter Pipeline (ICompositionRequestFilter chain)
  |
  v
CompositionHandler.HandleComposableRequest()
  +-> Resolve IViewModelFactory (endpoint-scoped if available, else global, else ExpandoObject)
  +-> Create view model, store in HttpContext.Items["composed-response-model"]
  +-> Store CompositionContext in HttpContext.Items["composition-context"]
  +-> Execute all IViewModelPreviewHandler.Preview() in parallel
  +-> Resolve all component instances from DI
  +-> Register ICompositionEventsSubscriber subscriptions on the CompositionContext
  +-> Execute all ICompositionRequestsHandler.Handle() in parallel (Task.WhenAll)
  |   (if no handlers found -> 404)
  |   (if exception -> invoke ICompositionErrorsHandler, then re-throw)
  +-> Finally: CleanupSubscribers()
  |
  v
Response Serialization
  - If IActionResult set via SetActionResult() and UseOutputFormatters=true -> Execute ActionResult
  - If UseOutputFormatters=true (no ActionResult) -> MVC output formatters (WriteModelAsync)
  - Otherwise (default) -> System.Text.Json, Content-Type: application/json; charset=utf-8
```

## 4. Composition Over Controllers (Alternate Path)

`CompositionOverControllersActionFilter` (MVC `IAsyncResultFilter`) intercepts after controller runs:

```text
Controller executes normally and produces a result
  |
  v
OnResultExecutionAsync fires
  - Matches the route against CompositionOverControllersRoutes
  - If composition handlers exist for this route:
    -> Performs model binding via ComponentsModelBinder
    -> Runs the full composition pipeline
    -> Merges the composed view model into:
       - ViewResult.ViewData.Model (MVC)
       - ObjectResult.Value (Web API)
  |
  v
Result executes with composed data
```
