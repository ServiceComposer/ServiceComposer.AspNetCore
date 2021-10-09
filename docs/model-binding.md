# Model Binding

_Available starting with v1.9.0_

Composition handlers get access to the incoming `HttpRequest`. If the incoming request contains a body, the handler can access a body like the following:

```json
{
    "AString": "Some value"
}
```

by using the following code:

<!-- snippet: net-core-3x-model-binding-raw-body-usage -->
<a id='snippet-net-core-3x-model-binding-raw-body-usage'></a>
```cs
[HttpPost("/sample/{id}")]
public async Task Handle(HttpRequest request)
{
    request.Body.Position = 0;
    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true );
    var body = await reader.ReadToEndAsync();
    var content = JObject.Parse(body);

    //use the content object instance as needed
}
```
<sup><a href='/src/Snippets.NetCore3x/ModelBinding/RawBodyUsageHandler.cs#L14-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-model-binding-raw-body-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Something similar applies to getting data from the route, or from the query string, or the incoming form. For example:

<!-- snippet: net-core-3x-model-binding-raw-route-data-usage -->
<a id='snippet-net-core-3x-model-binding-raw-route-data-usage'></a>
```cs
[HttpPost("/sample/{id}")]
public Task Handle(HttpRequest request)
{
    var routeData = request.HttpContext.GetRouteData();
    var id = int.Parse(routeData.Values["id"].ToString());

    //use the id value as needed

    return Task.CompletedTask;
}
```
<sup><a href='/src/Snippets.NetCore3x/ModelBinding/RawBodyUsageHandler.cs#L30-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-model-binding-raw-route-data-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Use model binding

It's possible to leverage ASP.Net built-in support for model binding to avoid accessing raw values of the incoming `HttpRequest`, such as the body.

To start using model binding configure the ASP.Net application to add MVC components:

<!-- snippet: net-core-3x-model-binding-add-controllers -->
<a id='snippet-net-core-3x-model-binding-add-controllers'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddViewModelComposition();
    services.AddControllers();
}
```
<sup><a href='/src/Snippets.NetCore3x/ModelBinding/ConfigureAppForModelBinding.cs#L8-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-model-binding-add-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> If, for reasons outside the scope of composing responses, there is the need to use MVC, or Razor Pages, or just controllers and views, any of the MVC configuration options will add support for model binding.

THe first step is to define the models, for example in the above sample the body model will look like the following C# class:

<!-- snippet: net-core-3x-model-binding-model -->
<a id='snippet-net-core-3x-model-binding-model'></a>
```cs
class BodyModel
{
    public string AString { get; set; }
}
```
<sup><a href='/src/Snippets.NetCore3x/ModelBinding/ModelBindingUsageHandler.cs#L8-L13' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-model-binding-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> The class name is irrelevant

Once we have a model for the body, a model that represent the incoming request is needed. In in a scenario in which there is the need to bind the body and the id from the route, the following request model can be used:

<!-- snippet: net-core-3x-model-binding-request -->
<a id='snippet-net-core-3x-model-binding-request'></a>
```cs
class RequestModel
{
    [FromRoute] public int id { get; set; }
    [FromBody] public BodyModel Body { get; set; }
}
```
<sup><a href='/src/Snippets.NetCore3x/ModelBinding/ModelBindingUsageHandler.cs#L15-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-model-binding-request' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> The class name is irrelevant. The name of the properties marked as `[FromRoute]` or `[FromQueryString]` must match the route data names or query string keys names. The name for the body, or form, property is irrelevant.

Once the models are defined they can be used as follows:

<!-- snippet: net-core-3x-model-binding-bind-body-and-route-data -->
<a id='snippet-net-core-3x-model-binding-bind-body-and-route-data'></a>
```cs
[HttpPost("/sample/{id}")]
public async Task Handle(HttpRequest request)
{
    var requestModel = await request.Bind<RequestModel>();
    var body = requestModel.Body;
    var aString = body.AString;
    var id = requestModel.id;

    //use values as needed
}
```
<sup><a href='/src/Snippets.NetCore3x/ModelBinding/ModelBindingUsageHandler.cs#L25-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-model-binding-bind-body-and-route-data' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For more information and options when using model binding refer to the [Microsoft official documentation](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-5.0).
