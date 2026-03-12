# OpenTelemetry

ServiceComposer emits [OpenTelemetry](https://opentelemetry.io/) traces using `System.Diagnostics.ActivitySource`, which is built into the .NET runtime. No additional NuGet packages are required in the application library itself.

Two independent activity sources are available — one per feature — so each can be opted into separately.

| Feature | Activity source name |
|---|---|
| ViewModel Composition | `ServiceComposer.AspNetCore.ViewModelComposition` |
| Scatter/Gather | `ServiceComposer.AspNetCore.ScatterGather` |

## Setup

Add the `OpenTelemetry.Extensions.Hosting` NuGet package, then register the desired source(s) with the tracing pipeline.

### ViewModel Composition only

<!-- snippet: open-telemetry-view-model-composition-setup -->
<a id='snippet-open-telemetry-view-model-composition-setup'></a>
```cs
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource("ServiceComposer.AspNetCore.ViewModelComposition"));
```
<sup><a href='/src/Snippets/OpenTelemetry/OpenTelemetrySetup.cs#L14-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-open-telemetry-view-model-composition-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Scatter/Gather only

<!-- snippet: open-telemetry-scatter-gather-setup -->
<a id='snippet-open-telemetry-scatter-gather-setup'></a>
```cs
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource("ServiceComposer.AspNetCore.ScatterGather"));
```
<sup><a href='/src/Snippets/OpenTelemetry/OpenTelemetrySetup.cs#L25-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-open-telemetry-scatter-gather-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Both features

<!-- snippet: open-telemetry-combined-setup -->
<a id='snippet-open-telemetry-combined-setup'></a>
```cs
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .AddSource("ServiceComposer.AspNetCore.ViewModelComposition")
        .AddSource("ServiceComposer.AspNetCore.ScatterGather"));
```
<sup><a href='/src/Snippets/OpenTelemetry/OpenTelemetrySetup.cs#L36-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-open-telemetry-combined-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## What gets traced

### ViewModel Composition

Each `ICompositionRequestsHandler` execution produces a child span of the ASP.NET Core HTTP server span.

| Attribute | Value |
|---|---|
| Span name | `composition.handler {FullyQualifiedTypeName}` |
| `composition.handler.type` | Fully qualified handler type name |
| `composition.handler.namespace` | Handler namespace |

When a handler raises an event via `context.RaiseEvent<TEvent>()`, the event handling produces a child span of the raising handler's span.

| Attribute | Value |
|---|---|
| Span name | `composition.event {FullyQualifiedEventTypeName}` |
| `composition.event.type` | Fully qualified event type name |
| `composition.event.namespace` | Event namespace |

Any span whose handler or event handler throws sets `ActivityStatusCode.Error` with the exception message as the status description.

### Scatter/Gather

Each `IGatherer` execution produces a child span of the ASP.NET Core HTTP server span.

| Attribute | Value |
|---|---|
| Span name | `scatter-gather.gatherer {Key}` |
| `scatter-gather.gatherer.key` | The gatherer key |

A gatherer that throws sets `ActivityStatusCode.Error` with the exception message as the status description.
