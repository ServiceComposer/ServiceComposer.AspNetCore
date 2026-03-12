---
title: 'ServiceComposer.AspNetCore — model binding, serialization, assembly scanning'
tags:
  - servicecomposer
  - model-binding
  - serialization
  - assembly-scanning
  - source-generator
lifecycle: permanent
createdAt: '2026-03-12T19:42:55.766Z'
updatedAt: '2026-03-12T19:42:55.766Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
## Model Binding

Three approaches, from simplest to most declarative:

### 1. Manual (imperative)

```csharp
var model = await request.Bind<T>();
var (model, isSet, modelState) = await request.TryBind<T>();
```

Requires MVC services (`AddControllers()` etc.).

### 2. Declarative via Attributes

```csharp
[HttpPost("/orders/{id}")]
[BindFromBody<OrderModel>]
[BindFromRoute<int>("id")]
public Task Handle(HttpRequest request)
{
    var ctx = request.GetCompositionContext();
    var args = ctx.GetArguments(this); // Experimental API (SC0001)
    var body = args.Argument<OrderModel>();
    var id = args.Argument<int>(name: "id");
}
```

### 3. Source-Generated (contract-less handlers)

Convention: class in `*.CompositionHandlers` namespace, name ends with `CompositionHandler`, method decorated with `[Http*]` attribute, returns `Task`. The source generator creates a wrapper implementing `ICompositionRequestsHandler` with appropriate `BindModel` attributes.

Source generator binding heuristics for method parameters (evaluated in order):

1. Parameter name matches route template placeholder → `BindFromRoute<T>`
2. Parameter has explicit `[FromBody]` → `BindFromBody<T>`
3. Parameter has explicit `[FromForm]` → `BindFromForm<T>`
4. Parameter has explicit `[FromQuery]` or is a simple type → `BindFromQuery<T>`
5. Complex type without explicit binding attribute → `Bind<T>` (multi-source, respects property-level attributes like `[FromRoute]`)

### Binding Attributes

| Attribute | Binding Source |
| --- | --- |
| `BindAttribute<T>` | `BindingSource.ModelBinding` (multi-source) |
| `BindFromBodyAttribute<T>` | `BindingSource.Body` |
| `BindFromRouteAttribute<T>(key)` | `BindingSource.Path` |
| `BindFromQueryAttribute<T>(name)` | `BindingSource.Query` |
| `BindFromFormAttribute<T>(name?)` | `BindingSource.Form` |

## Response Serialization

| Mode | Behavior |
| --- | --- |
| Default | `System.Text.Json` with configurable casing (CamelCase default, PascalCase option) |
| Custom Settings | `options.ResponseSerialization.UseCustomJsonSerializerSettings(Func<HttpRequest, JsonSerializerOptions>)` |
| Output Formatters | `options.ResponseSerialization.UseOutputFormatters = true` — uses MVC output formatter pipeline |
| Accept-Casing Header | Client can override with `Accept-Casing: casing/pascal` or `casing/camel` |

## Assembly Scanning and Component Discovery

`AssemblyScanner` (enabled by default) scans loaded assemblies:

- Uses `DependencyContext` to enumerate runtime libraries
- Validates PE files via `System.Reflection.Metadata` (filters out non-.NET assemblies)
- Discovers and registers: `ICompositionRequestsHandler`, `ICompositionEventsSubscriber`, `IViewModelPreviewHandler`, `IViewModelFactory`, `IEndpointScopedViewModelFactory`, `ICompositionRequestFilter`, `ICompositionEventsHandler<T>`, and contract-less composition handlers

Can be disabled with `options.AssemblyScanner.Disable()` for explicit registration via `options.RegisterCompositionHandler<T>()`.
