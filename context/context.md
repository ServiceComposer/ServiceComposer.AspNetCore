# ServiceComposer.AspNetCore - Codebase Context

## What is ServiceComposer?

ServiceComposer is a **ViewModel Composition Gateway** for ASP.NET Core. It solves the problem of displaying data owned by multiple autonomous services in SOA/microservices architectures without violating service boundaries, sharing databases, or creating distributed monoliths.

Each service contributes its own data to a shared dynamic view model at the API gateway level. Handlers execute in parallel, and the composed result is serialized back to the caller. This is the "read side" counterpart to NServiceBus messaging (the "write side").

**Author:** Mauro Servienti
**License:** Apache 2.0
**Target Framework:** .NET 8.0
**Current Major Version:** 4.x (MinVer minimum: 4.0)

## Repository Structure

```
src/
  ServiceComposer.AspNetCore/                  # Main library
  ServiceComposer.AspNetCore.Tests/            # Integration tests (xUnit, .NET 8/9)
  ServiceComposer.AspNetCore.SourceGeneration/ # Incremental source generator (netstandard2.0)
  ServiceComposer.AspNetCore.SourceGeneration.Tests/
  TestClassLibraryWithHandlers/                # Test helper library
  Snippets/                                    # Documentation code snippets
docs/                                          # Markdown documentation
nugets/                                        # Package output directory
.github/workflows/                             # CI/CD (Windows + Linux, .NET 8/9)
```

The source generator ships inside the main NuGet package (in the `analyzers/dotnet/cs` folder).

## Key Interfaces

### Request Handling

| Interface | Purpose |
|---|---|
| `ICompositionRequestsHandler` | Core handler: `Task Handle(HttpRequest request)` - contributes data to the view model |
| `ICompositionEventsSubscriber` | Route-scoped event subscriber: `void Subscribe(ICompositionEventsPublisher)` |
| `ICompositionEventsHandler<T>` | Global event handler: `Task Handle(T @event, HttpRequest)` - handles events from any route |
| `ICompositionEventsPublisher` | Event bus within a single request: `void Subscribe<T>(CompositionEventHandler<T>)` |
| `ICompositionContext` | Request context: provides `RequestId`, `RaiseEvent<T>()`, and `GetArguments()` for model binding |

### View Model Creation

| Interface | Purpose |
|---|---|
| `IViewModelFactory` | Creates the view model object for a request (default: `ExpandoObject`) |
| `IEndpointScopedViewModelFactory` | Marker extending `IViewModelFactory` - one per endpoint, overrides global factory |
| `IViewModelPreviewHandler` | Visitor pattern: `Task Preview(HttpRequest)` - inspect/modify the view model before handlers run |

### Filtering

| Interface | Purpose |
|---|---|
| `ICompositionRequestFilter` | Filter wrapping composition handler execution |
| `ICompositionRequestFilter<T>` | Type-specific filter for a particular handler type |
| `CompositionRequestFilterAttribute` | Attribute-based filter applied to handler methods |
| ASP.NET Core `IEndpointFilter` | Standard endpoint filters, applied via `MapCompositionHandlers().AddEndpointFilter()` |

### Error Handling

| Interface | Purpose |
|---|---|
| `ICompositionErrorsHandler` | `Task OnRequestError(HttpRequest, Exception)` - invoked when a handler throws |

### Extensibility

| Interface | Purpose |
|---|---|
| `IViewModelCompositionOptionsCustomization` | Plugin interface for external assemblies to customize options during scanning |

## Request Pipeline (Detailed Flow)

### 1. Startup Registration

```
services.AddViewModelComposition() -> ViewModelCompositionOptions
  - Assembly scanning discovers types implementing key interfaces
  - Registers them as transient services in DI
  - Populates CompositionMetadataRegistry with component types and event handler mappings
```

### 2. Endpoint Mapping

```
endpoints.MapCompositionHandlers()
  - Groups all registered components by route template (from [HttpGet], [HttpPost], etc.)
  - For each unique route+method combination, creates a CompositionEndpointBuilder
  - If CompositionOverControllers is enabled AND a controller already owns the route,
    stores those handler types and their method metadata in CompositionOverControllersRoutes instead
  - Registers endpoints via CompositionEndpointDataSource
```

### 3. Request Arrives at a Composition Endpoint

```
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
  - Shared by both composition endpoints and composition over controllers
  |
  v
Endpoint Filter Pipeline (ASP.NET Core IEndpointFilter chain, cached after first build)
  |
  v
Composition Request Filter Pipeline (ICompositionRequestFilter chain)
  - Attribute-based filters from method metadata
  - Type-based filters (ICompositionRequestFilter<T>) from DI
  |
  v
CompositionHandler.HandleComposableRequest()
  |
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
  - If IActionResult was set via request.SetActionResult() and UseOutputFormatters=true
    -> Execute the ActionResult
  - If UseOutputFormatters=true (no ActionResult)
    -> Use MVC output formatters (WriteModelAsync)
  - Otherwise (default path)
    -> System.Text.Json serialization with casing support
    -> Content-Type: application/json; charset=utf-8
```

### 4. Composition Over Controllers (Alternate Path)

When enabled, `CompositionOverControllersActionFilter` (an MVC `IAsyncResultFilter`) intercepts:

```
Controller executes normally and produces a result
  |
  v
OnResultExecutionAsync fires
  - Matches the route against CompositionOverControllersRoutes
  - If composition handlers exist for this route:
    -> Performs model binding via ComponentsModelBinder using stored handler metadata
    -> Runs the full composition pipeline (same as above)
    -> Merges the composed view model into:
       - ViewResult.ViewData.Model (MVC)
       - ObjectResult.Value (Web API)
  |
  v
Result executes with composed data
```

Both contract-based (`ICompositionRequestsHandler`) and contract-less composition handlers are supported in this mode.

## Composition Events System

Events are the mechanism for multi-step composition (e.g., list composition where one service provides IDs and others load data for those IDs).

### Two subscription mechanisms:

1. **Route-scoped** (`ICompositionEventsSubscriber`): Handler implements `Subscribe()` with an `[Http*]` route attribute. Subscriptions are registered per-request on the `CompositionContext`.

2. **Global** (`ICompositionEventsHandler<T>`): Registered in DI and `CompositionMetadataRegistry`. Invoked for any route when the matching event type is raised.

### Event flow:

```
Handler calls: await context.RaiseEvent(new MyEvent { ... })
  |
  +-> Look up ICompositionEventsHandler<MyEvent> types in metadata registry
  |   Resolve from DI, invoke Handle()
  |
  +-> Look up route-scoped subscriptions in ConcurrentDictionary
  |   Invoke registered CompositionEventHandler<T> delegates
  |
  +-> Task.WhenAll() on all event handlers
```

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
|---|---|
| `BindAttribute<T>` | `BindingSource.ModelBinding` (multi-source) |
| `BindFromBodyAttribute<T>` | `BindingSource.Body` |
| `BindFromRouteAttribute<T>(key)` | `BindingSource.Path` |
| `BindFromQueryAttribute<T>(name)` | `BindingSource.Query` |
| `BindFromFormAttribute<T>(name?)` | `BindingSource.Form` |

## Response Serialization

| Mode | Behavior |
|---|---|
| Default | `System.Text.Json` with configurable casing (CamelCase default, PascalCase option) |
| Custom Settings | `options.ResponseSerialization.UseCustomJsonSerializerSettings(Func<HttpRequest, JsonSerializerOptions>)` |
| Output Formatters | `options.ResponseSerialization.UseOutputFormatters = true` - uses MVC output formatter pipeline |
| Accept-Casing Header | Client can override with `Accept-Casing: casing/pascal` or `casing/camel` |

## Assembly Scanning & Component Discovery

`AssemblyScanner` (enabled by default) scans loaded assemblies:
- Uses `DependencyContext` to enumerate runtime libraries
- Validates PE files via `System.Reflection.Metadata` (filters out non-.NET assemblies)
- Discovers and registers: `ICompositionRequestsHandler`, `ICompositionEventsSubscriber`, `IViewModelPreviewHandler`, `IViewModelFactory`, `IEndpointScopedViewModelFactory`, `ICompositionRequestFilter`, `ICompositionEventsHandler<T>`, and contract-less composition handlers

Can be disabled with `options.AssemblyScanner.Disable()` for explicit registration via `options.RegisterCompositionHandler<T>()`.

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

## Important Implementation Details

### View Model Storage
The composed view model lives in `HttpContext.Items["composed-response-model"]`. The composition context lives in `HttpContext.Items["composition-context"]`. The composed request ID is a header: `composed-request-id`.

### Handler Lifetime
All composition components are registered as **transient** in DI. A new instance is resolved per request.

### Parallel Execution
- All `ICompositionRequestsHandler.Handle()` calls run in parallel via `Task.WhenAll`
- All `IViewModelPreviewHandler.Preview()` calls run in parallel
- All event handlers (both global and route-scoped) run in parallel via `Task.WhenAll`

### Thread Safety
- `ExpandoObject` is per-request scoped (no cross-request sharing)
- `CompositionContext` uses `ConcurrentDictionary` for event subscriptions
- Endpoint filter pipeline is built once and cached (with lock)

### Error Handling
When a handler throws during `Task.WhenAll`, all `ICompositionErrorsHandler` instances are invoked sequentially, then the exception is re-thrown.

### Contract-less Composition Handlers
A convention-based alternative to implementing `ICompositionRequestsHandler`. Rules:
- Class namespace is `CompositionHandlers` or ends with `.CompositionHandlers`
- Class name ends with `CompositionHandler`
- Public method decorated with `[Http*]` attribute returning `Task`
- Source generator creates a wrapper class implementing the interface

### Body Buffering
`request.EnableBuffering()` is called early in the pipeline so multiple handlers can independently read and bind from the request body.

## NuGet Dependencies

| Package | Range | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.App` | Framework ref | ASP.NET Core |
| `System.ValueTuple` | [4.5.0, 5.0.0) | Legacy tuple support |
| `Microsoft.Extensions.DependencyModel` | [8.0.0, 10.0.0) | Assembly scanning |
| `System.Reflection.Metadata` | [8.0.0, 10.0.0) | PE file validation |
| `System.Text.Json` | [8.0.5, 10.0.0) | JSON serialization (security update) |
| `MinVer` | 7.0.0 | Semantic versioning from git tags |
| `Microsoft.SourceLink.GitHub` | 8.0.0 | Source debugging |

## CI/CD

- GitHub Actions: Windows + Linux matrix
- .NET SDKs: 8.0.x and 9.0.x
- Versioning: MinVer from git tags (pattern `[0-9].[0-9]+.[0-9]`)
- Packages published to NuGet (releases) and Feedz.io (pre-releases)

## Source Files Map

### Core Pipeline
| File | Key Type/Method |
|---|---|
| `ServiceCollectionExtensions.cs` | `AddViewModelComposition()` |
| `ViewModelCompositionOptions.cs` | `ViewModelCompositionOptions` - registration orchestrator |
| `EndpointsExtensions.cs` | `MapCompositionHandlers()` - endpoint mapping |
| `CompositionEndpointBuilder.cs` | `Build()` - creates the endpoint request delegate |
| `ComponentsModelBinder.cs` | `BindAll()` - shared model binding for all composition paths |
| `CompositionEndpointBuilder.BindingArguments.cs` | `GetAllComponentsArguments()` - delegates to `ComponentsModelBinder` |
| `CompositionEndpointBuilder.CompositionFilters.cs` | Composition filter pipeline builder |
| `CompositionEndpointBuilder.EndpointFilters.cs` | Endpoint filter pipeline builder (cached) |
| `CompositionEndpointDataSource.cs` | Custom `EndpointDataSource` implementation |
| `CompositionHandler.cs` | `HandleComposableRequest()` - core composition orchestration |
| `CompositionContext.cs` | `ICompositionContext` + `ICompositionEventsPublisher` impl |

### Interfaces & Contracts
| File | Interface |
|---|---|
| `ICompositionRequestsHandler.cs` | Handler contract |
| `ICompositionEventsSubscriber.cs` | Route-scoped event subscriber |
| `ICompositionEventsHandler.cs` | Global event handler |
| `ICompositionEventsPublisher.cs` | Event publishing contract |
| `ICompositionContext.cs` | Request composition context |
| `IViewModelFactory.cs` | View model factory |
| `IEndpointScopedViewModelFactory.cs` | Per-endpoint factory marker |
| `IViewModelPreviewHandler.cs` | Preview/visitor handler |
| `ICompositionErrorsHandler.cs` | Error handling hook |
| `ICompositionRequestFilter.cs` | Filter contract |

### Model Binding
| File | Purpose |
|---|---|
| `ModelBinding/BindModelAttribute.cs` | Base + sealed binding attribute hierarchy |
| `ModelBinding/RequestModelBinder.cs` | Wraps ASP.NET Core model binding |
| `ModelBinding/HttpRequestModelBinderExtension.cs` | `Bind<T>()` / `TryBind<T>()` extensions |
| `ModelBindingArgument.cs` | Bound argument DTO |
| `ModelBindingArgumentExtensions.cs` | `Argument<T>()` search helpers |

### Extensions & HTTP
| File | Purpose |
|---|---|
| `HttpRequestExtensions.cs` | `GetComposedResponseModel()`, `SetActionResult()`, `GetCompositionContext()` |
| `HttpContextExtensions.cs` | `EnsureRequestIdIsSetup()` |
| `HttpContextActionResultExtensions.cs` | `WriteModelAsync<T>()`, `ExecuteResultAsync()` |
| `ComposedRequestIdHeader.cs` | Header constant: `"composed-request-id"` |

### MVC Integration
| File | Purpose |
|---|---|
| `CompositionOverControllersActionFilter.cs` | `IAsyncResultFilter` - intercepts controller results |
| `CompositionOverControllersRoutes.cs` | Registry of routes with composition handlers and their method metadata |
| `CompositionOverControllersOptions.cs` | `IsEnabled`, `UseCaseInsensitiveRouteMatching` |

### Discovery
| File | Purpose |
|---|---|
| `AssemblyScanner.cs` | Assembly discovery and loading |
| `AssemblyValidator.cs` | PE file validation |
| `CompositionMetadataRegistry.cs` | `HashSet<Type> Components` + `Dictionary<Type, List<Type>> EventHandlers` |

### Serialization & Options
| File | Purpose |
|---|---|
| `ResponseSerializationOptions.cs` | Casing, custom settings, output formatters toggle |
| `CompositionRequestFilterAttribute.cs` | Attribute-based filter base class |
| `CompositionRequestFilterContext.cs` | Context for composition filters |

## Design Patterns

1. **Parallel Scatter-Gather**: Handlers execute in parallel, contribute to shared view model
2. **Observer/Pub-Sub**: In-request composition events
3. **Factory**: `IViewModelFactory` / `IEndpointScopedViewModelFactory`
4. **Visitor**: `IViewModelPreviewHandler`
5. **Pipeline/Filter Chain**: Both endpoint filters and composition request filters
6. **Registry**: `CompositionMetadataRegistry`
7. **Convention over Configuration**: Contract-less handlers + source generation

## TODOs in Code

- `CompositionHandler.cs:31` - Shortcut to 404 if no handlers
- `CompositionHandler.cs:38` - Second 404 shortcut
- `CompositionHandler.cs:42` - Apply composition filter per-handler, not before whole composition
- `CompositionEndpointBuilder.cs:121` - Source-generate convention-based filter invocation context
- `ComponentsModelBinder.cs:33` - Cache RequestModelBinder instance
- `ComponentsModelBinder.cs:40` - Throw if binding failed
