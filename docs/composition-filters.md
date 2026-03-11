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
    public override ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
    {
        return next(context);
    }
}
```
<sup><a href='/src/Snippets/CompositionFilters/SampleHandler.cs#L8-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-filter-attribute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> [!NOTE]
> The `InvokeAsync` implementation is responsible to invoke the next filter in the pipeline.

Apply the attribute to the composition handlers to intercept:

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
    public ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
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

## When to use composition filters vs endpoint filters

ServiceComposer provides two filter extension points. Choosing the right one depends on the scope of interception needed.

**Use [endpoint filters](endpoint-filters.md) when:**

- The logic applies to the entire composed request regardless of which handlers are involved (e.g. request logging, timing, global validation).
- You want to short-circuit the whole composition before any handler runs.
- The logic is independent of specific handler types.

**Use composition filters when:**

- The logic is specific to one particular composition handler type.
- Different handlers on the same route need different pre/post processing (e.g. handler-specific authorization checks, handler-specific input validation).
- You want to use the attribute form (`[SampleCompositionFilter]`) to keep the filter declaration co-located with the handler method.

In short: endpoint filters are coarse-grained (the whole endpoint), composition filters are fine-grained (a specific handler).
