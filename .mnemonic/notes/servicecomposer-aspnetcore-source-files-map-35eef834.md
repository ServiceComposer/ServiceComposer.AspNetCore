---
title: ServiceComposer.AspNetCore — source files map
tags:
  - servicecomposer
  - source-files
  - file-map
  - codebase
lifecycle: permanent
createdAt: '2026-03-12T19:43:35.851Z'
updatedAt: '2026-03-12T19:43:35.851Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
## Core Pipeline

| File | Key Type/Method |
| --- | --- |
| `ServiceCollectionExtensions.cs` | `AddViewModelComposition()` |
| `ViewModelCompositionOptions.cs` | `ViewModelCompositionOptions` — registration orchestrator |
| `EndpointsExtensions.cs` | `MapCompositionHandlers()` — endpoint mapping |
| `CompositionEndpointBuilder.cs` | `Build()` — creates the endpoint request delegate |
| `ComponentsModelBinder.cs` | `BindAll()` — shared model binding for all composition paths |
| `CompositionEndpointBuilder.BindingArguments.cs` | `GetAllComponentsArguments()` — delegates to `ComponentsModelBinder` |
| `CompositionEndpointBuilder.CompositionFilters.cs` | Composition filter pipeline builder |
| `CompositionEndpointBuilder.EndpointFilters.cs` | Endpoint filter pipeline builder (cached) |
| `CompositionEndpointDataSource.cs` | Custom `EndpointDataSource` implementation |
| `CompositionHandler.cs` | `HandleComposableRequest()` — core composition orchestration |
| `CompositionContext.cs` | `ICompositionContext` + `ICompositionEventsPublisher` impl |

## Interfaces and Contracts

| File | Interface |
| --- | --- |
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

## Model Binding

| File | Purpose |
| --- | --- |
| `ModelBinding/BindModelAttribute.cs` | Base + sealed binding attribute hierarchy |
| `ModelBinding/RequestModelBinder.cs` | Wraps ASP.NET Core model binding |
| `ModelBinding/HttpRequestModelBinderExtension.cs` | `Bind<T>()` / `TryBind<T>()` extensions |
| `ModelBindingArgument.cs` | Bound argument DTO |
| `ModelBindingArgumentExtensions.cs` | `Argument<T>()` search helpers |

## Extensions and HTTP

| File | Purpose |
| --- | --- |
| `HttpRequestExtensions.cs` | `GetComposedResponseModel()`, `SetActionResult()`, `GetCompositionContext()` |
| `HttpContextExtensions.cs` | `EnsureRequestIdIsSetup()` |
| `HttpContextActionResultExtensions.cs` | `WriteModelAsync<T>()`, `ExecuteResultAsync()` |
| `ComposedRequestIdHeader.cs` | Header constant: `"composed-request-id"` |

## MVC Integration

| File | Purpose |
| --- | --- |
| `CompositionOverControllersActionFilter.cs` | `IAsyncResultFilter` — intercepts controller results |
| `CompositionOverControllersRoutes.cs` | Registry of routes with composition handlers and their method metadata |
| `CompositionOverControllersOptions.cs` | `IsEnabled`, `UseCaseInsensitiveRouteMatching` |

## Discovery

| File | Purpose |
| --- | --- |
| `AssemblyScanner.cs` | Assembly discovery and loading |
| `AssemblyValidator.cs` | PE file validation |
| `CompositionMetadataRegistry.cs` | `HashSet<Type> Components` + `Dictionary<Type, List<Type>> EventHandlers` |

## Serialization and Options

| File | Purpose |
| --- | --- |
| `ResponseSerializationOptions.cs` | Casing, custom settings, output formatters toggle |
| `CompositionRequestFilterAttribute.cs` | Attribute-based filter base class |
| `CompositionRequestFilterContext.cs` | Context for composition filters |
