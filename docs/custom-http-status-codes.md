# Custom HTTP status codes in ASP.NET Core 3.x

The response status code can be set in requests handlers and it'll be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

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

NOTE: Requests handlers are executed in parallel in a non-deterministic way, setting the response code in more than one handler can have unpredictable effects.
