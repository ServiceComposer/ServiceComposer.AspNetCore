# ASP.Net MVC Action results

MVC Action results support allow composition handlers to set custom response results for specific scenarios, like for example, handling bad requests or validation error that would normally require throwing an exception. Setting a custom action result is done by using the `SetActionResult()` `HttpRequest` extension method:

<!-- snippet: action-results -->
<a id='snippet-action-results'></a>
```cs
public class UseSetActionResultHandler : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var id = request.RouteValues["id"];

        //validate the id format

        var problems = new ValidationProblemDetails(new Dictionary<string, string[]>()
        {
            { "Id", new []{ "The supplied id does not respect the identifier format." } }
        });
        var result = new BadRequestObjectResult(problems);

        request.SetActionResult(result);

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/ActionResult/UseSetActionResultHandler.cs#L10-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-action-results' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Using MVC action results require enabling output formatters support:

<!-- snippet: action-results-required-config -->
<a id='snippet-action-results-required-config'></a>
```cs
services.AddViewModelComposition(options =>
{
    options.ResponseSerialization.UseOutputFormatters = true;
});
```
<sup><a href='/src/Snippets/ActionResult/UseSetActionResultHandler.cs#L37-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-action-results-required-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> [!NOTE]
> ServiceComposer supports only one action result per request. If two or more composition handlers try to set action results, only the first one will succeed and subsequent requests will be ignored.
