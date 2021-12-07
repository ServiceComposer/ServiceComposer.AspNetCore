# Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For example, it's possible to use the `Authorize` attribute to specify that a handler requires authorization. The authorization process is the regular ASP.NET Core 3.x process and no special configuration is needed to plugin ServiceComposer:

<!-- snippet: sample-handler-with-authorization -->
<a id='snippet-net-core-3x-sample-handler-with-authorization'></a>
```cs
public class SampleHandlerWithAuthorization : ICompositionRequestsHandler
{
    [Authorize]
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/SampleHandler/SampleHandler.cs#L10-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-sample-handler-with-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
