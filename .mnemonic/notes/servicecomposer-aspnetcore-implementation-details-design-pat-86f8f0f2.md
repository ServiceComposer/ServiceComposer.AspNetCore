---
title: >-
  ServiceComposer.AspNetCore — implementation details, design patterns, and
  TODOs
tags:
  - servicecomposer
  - implementation
  - design-patterns
  - thread-safety
  - events
  - todos
lifecycle: permanent
createdAt: '2026-03-12T19:43:16.417Z'
updatedAt: '2026-03-12T19:43:16.417Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
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

### Composition Events

Events are the mechanism for multi-step composition (e.g., list composition where one service provides IDs and others load data for those IDs).

Two subscription mechanisms:

1. **Route-scoped** (`ICompositionEventsSubscriber`): Handler implements `Subscribe()` with an `[Http*]` route attribute. Subscriptions are registered per-request on the `CompositionContext`.
2. **Global** (`ICompositionEventsHandler<T>`): Registered in DI and `CompositionMetadataRegistry`. Invoked for any route when the matching event type is raised.

Event flow:

```text
Handler calls: await context.RaiseEvent(new MyEvent { ... })
  +-> Look up ICompositionEventsHandler<MyEvent> types in metadata registry
  |   Resolve from DI, invoke Handle()
  +-> Look up route-scoped subscriptions in ConcurrentDictionary
  |   Invoke registered CompositionEventHandler<T> delegates
  +-> Task.WhenAll() on all event handlers
```

## Design Patterns

1. **Parallel Scatter-Gather**: Handlers execute in parallel, contribute to shared view model
2. **Observer/Pub-Sub**: In-request composition events
3. **Factory**: `IViewModelFactory` / `IEndpointScopedViewModelFactory`
4. **Visitor**: `IViewModelPreviewHandler`
5. **Pipeline/Filter Chain**: Both endpoint filters and composition request filters
6. **Registry**: `CompositionMetadataRegistry`
7. **Convention over Configuration**: Contract-less handlers + source generation

## TODOs in Code

- `CompositionHandler.cs:31` — Shortcut to 404 if no handlers
- `CompositionHandler.cs:38` — Second 404 shortcut
- `CompositionHandler.cs:42` — Apply composition filter per-handler, not before whole composition
- `CompositionEndpointBuilder.cs:121` — Source-generate convention-based filter invocation context
- `ComponentsModelBinder.cs:33` — Cache RequestModelBinder instance
- `ComponentsModelBinder.cs:40` — Throw if binding failed
