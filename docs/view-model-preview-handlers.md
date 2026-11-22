# View model preview handlers

A view model preview handler runs for every composition request, after the view model is created and before any composition handler runs. It's the place for what every route needs, e.g., adding data common to all the responses, or initializing a [strongly typed view model](view-model-factory.md) created by a global factory.

A view model preview handler implements the `IViewModelPreviewHandler` interface:

<!-- snippet: view-model-preview-handler -->
<a id='snippet-view-model-preview-handler'></a>
```cs
public class RequestInfoPreviewHandler : IViewModelPreviewHandler
{
    public Task Preview(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();
        vm.RequestId = request.GetCompositionContext().RequestId;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/ViewModelPreview/RequestInfoPreviewHandler.cs#L9-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-view-model-preview-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## When preview handlers run

For each composition request:

1. The view model is created, by a [view model factory](view-model-factory.md) or as the default `dynamic` one, and the composition context is set up. `request.GetComposedResponseModel()` and `request.GetCompositionContext()` are available to preview handlers.
2. All the preview handlers run, in parallel.
3. The route's event subscribers subscribe, and its composition handlers run.

Preview handlers run for every route, including the routes [composed over controllers](composition-over-controllers.md). They aren't bound to a route: to do something only for some routes, check the request, e.g., `request.Path`.

> [!NOTE]
> Preview handlers run in parallel with each other, and all of them write to the same view model. Each preview handler should write its own properties. See [thread safety](thread-safety.md).

## Registering preview handlers

Preview handlers are discovered by assembly scanning, and by the [generated components providers](registering-components.md), and registered in the DI container as transient components. Hence, they support dependency injection.

When neither is used, register them in the DI container:

<!-- snippet: view-model-preview-handler-registration -->
<a id='snippet-view-model-preview-handler-registration'></a>
```cs
builder.Services.AddViewModelComposition(options =>
{
    options.AssemblyScanner.Disable();
});
builder.Services.AddTransient<IViewModelPreviewHandler, RequestInfoPreviewHandler>();
```
<sup><a href='/src/Snippets/ViewModelPreview/RequestInfoPreviewHandler.cs#L28-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-view-model-preview-handler-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
