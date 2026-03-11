# Threading and shared resources

ServiceComposer executes all composition handlers for a given request **in parallel** via `Task.WhenAll`. This means multiple handlers are running concurrently within the scope of a single HTTP request.

## Handler lifetime

All composition components — handlers, event subscribers, and event handlers — are registered in DI as **transient**. Each incoming request receives a new, independent instance of each handler. Two concurrent HTTP requests never share handler instances.

## The shared view model

Within a single request, all handlers share one view model object (the `dynamic` `ExpandoObject` by default, or a strongly typed instance if a view model factory is configured). Because handlers run in parallel, any code that writes to the view model is subject to concurrent access.

`ExpandoObject` is **not thread-safe**. If two handlers write different properties at the same time, the writes are typically safe in practice because they target independent properties — but any code that reads and then conditionally writes the same property, or iterates the object, must be protected.

> [!WARNING]
> Setting the same property from two different handlers produces a data race. Design your composition so each service owns distinct, non-overlapping properties on the view model.

## Singleton and scoped dependencies

DI-registered services injected into handlers follow their own registered lifetimes. A **singleton** service is shared across all requests and all handlers; it must be thread-safe. A **scoped** service registered via the standard ASP.NET Core request scope is created once per HTTP request, but since multiple handlers within that request run concurrently, the scoped instance is still shared among them.

If a dependency (e.g. a `DbContext`) is not safe for concurrent use, do not inject it as a scoped service and share it across handlers. Instead, resolve a new instance for each handler by creating a child DI scope:

<!-- snippet: thread-safety-child-scope -->
<a id='snippet-thread-safety-child-scope'></a>
```cs
public class SalesHandler : ICompositionRequestsHandler
{
    readonly IServiceProvider _serviceProvider;

    public SalesHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [HttpGet("/product/{id}")]
    public async Task Handle(HttpRequest request)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var vm = request.GetComposedResponseModel();
        vm.ProductPrice = await db.GetProductPriceAsync(
            request.RouteValues["id"].ToString());
    }
}
```
<sup><a href='/src/Snippets/ThreadSafety/ScopedDependencyHandler.cs#L17-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-thread-safety-child-scope' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`CreateAsyncScope()` creates a short-lived child scope whose lifetime is tied to the `await using` block. The scoped service resolved from it is private to this handler invocation and not shared with any other handler running in parallel.

## Composition events

Event handlers (`ICompositionEventsHandler<T>` and route-scoped subscribers) also run in parallel with each other and with composition handlers. The same threading considerations apply: each event handler instance is transient, but they all share the single request-scoped view model.
