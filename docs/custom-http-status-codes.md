# Custom HTTP status codes

The response status code can be set in composition handlers and it will be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

<!-- snippet: sample-handler-with-custom-status-code -->
<a id='snippet-sample-handler-with-custom-status-code'></a>
```cs
public class SampleHandlerWithCustomStatusCode : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        var response = request.HttpContext.Response;
        response.StatusCode = (int)HttpStatusCode.Forbidden;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/SampleHandler/SampleHandler.cs#L22-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-handler-with-custom-status-code' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> [!WARNING]
> Composition handlers execute in parallel in a non-deterministic order. If more than one handler sets the response status code, the final code written to the response is unpredictable. Only one handler in a composition group should set the status code.

## Recommended approach: use action results instead

For error scenarios (validation failures, not found, forbidden), prefer using [MVC Action results](action-results.md) via `request.SetActionResult(result)`. Action results are designed for exactly this case — ServiceComposer guarantees only the first handler to call `SetActionResult` takes effect, making the behavior deterministic:

<!-- snippet: set-action-result-preferred -->
<a id='snippet-set-action-result-preferred'></a>
```cs
public class SampleHandler : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        if (!IsValid(request))
        {
            request.SetActionResult(new BadRequestResult());
            return Task.CompletedTask;
        }

        var vm = request.GetComposedResponseModel();
        vm.Data = "...";
        return Task.CompletedTask;
    }

    bool IsValid(HttpRequest request) => request.RouteValues["id"] != null;
}
```
<sup><a href='/src/Snippets/SampleHandler/SetActionResultHandler.cs#L8-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-set-action-result-preferred' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Setting a status code directly on `HttpContext.Response` is appropriate only when a handler is the sole authority on the response code for its route, or when using a non-MVC endpoint where action results are not available.
