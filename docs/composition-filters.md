# Composition requests filters

_Available starting with v2.3.0_

Composition requests filters allow intercepting composition requests to specific composition handlers. Contrary to [endpoint filters](endpoint-filters.md) that are generic HTTP filters, composition filters sit right in front of the composition handlers they are configured to intercept.

## Defining composition requests filters

Composition requests filter can be defined as attributes or as classes.

### Defining composition requests filters as attributes

Create an attribute that inherits from `CompositionRequestFilterAttribute` like in the following snippet:

<!-- snippet: composition-filter-attribute -->
<a id='snippet-composition-filter-attribute'></a>
```cs
public class SampleCompositionFilterAttribute : CompositionRequestFilterAttribute
{
    public override ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
    {
        return next(context);
    }
}
```
<sup><a href='/src/Snippets/CompositionFilters/SampleHandler.cs#L8-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-filter-attribute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> [!NOTE]
> The `InvokeAsync` implementation is responsible to invoke the next filter in the pipeline. Not invoking it skips the intercepted composition handler.

Apply the attribute to the composition handlers to intercept, on their method or their class:

<!-- snippet: handler-with-composition-filter -->
<a id='snippet-handler-with-composition-filter'></a>
```cs
public class SampleHandler : ICompositionRequestsHandler
{
    [SampleCompositionFilter]
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/CompositionFilters/SampleHandler.cs#L28-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-handler-with-composition-filter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Defining composition requests filters as classes

Create a class that implements the `ICompositionRequestFilter<T>` interface, where the generic `T` parameter is the composition handler type to intercept:

<!-- snippet: composition-filter-class -->
<a id='snippet-composition-filter-class'></a>
```cs
public class SampleCompositionFilter : ICompositionRequestFilter<SampleHandler>
{
    public ValueTask InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
    {
        return next(context);
    }
}
```
<sup><a href='/src/Snippets/CompositionFilters/SampleHandler.cs#L18-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-filter-class' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The above snippet defines a filter intercepting requests to the `SampleHandler` composition handler.

> [!NOTE]
> Filters defined as classes implementing the `ICompositionRequestFilter<T>` interface will be automatically registered in DI as transient, and can use DI to resolve dependencies.

## How composition filters run

_Starting with v6.0.0_

Each composition handler runs behind its own filters, right before it. For a route with several composition handlers:

- The filters of a composition handler wrap that handler only: the other handlers of the route run behind their own filters, or none.
- A filter not invoking `next` skips its composition handler only. The other handlers still run, and the response is still the composed view model. To replace the response, set an action result with `SetActionResult`. To fail the request, throw an exception. To stop the request before any composition handler runs, use an [endpoint filter](endpoint-filters.md).
- A composition handler's filters run in this order: the attributes on its class, the attributes on its method, then the classes implementing `ICompositionRequestFilter<T>` for it, in registration order.
- Composition handlers run concurrently, and so do their filters. Filters of different handlers that share state, such as the view model, need to be safe to run concurrently.
- `CompositionRequestFilterContext.Handler` is the composition handler the filter is invoked for.
- Filters also run when composition handlers [compose over controllers](composition-over-controllers.md).

Before v6.0.0, all the composition filters of a route wrapped the whole composition. See the [upgrade guide](upgrade-guides/5.x.x-6.0.0.md#composition-filters-run-per-composition-handler).

## When to use composition filters vs endpoint filters

ServiceComposer provides two filter extension points. Choosing the right one depends on the scope of interception needed.

**Use [endpoint filters](endpoint-filters.md) when:**

- The logic applies to the entire composed request regardless of which handlers are involved (e.g. request logging, timing, global validation).
- You want to short-circuit the whole composition before any handler runs, or to change the response.
- The logic is independent of specific handler types.

**Use composition filters when:**

- The logic is specific to one particular composition handler type.
- Different handlers on the same route need different pre/post processing (e.g. handler-specific authorization checks, handler-specific input validation).
- You want to use the attribute form (`[SampleCompositionFilter]`) to keep the filter declaration co-located with the handler method.

In short: endpoint filters are coarse-grained (the whole endpoint), composition filters are fine-grained (a specific handler).
