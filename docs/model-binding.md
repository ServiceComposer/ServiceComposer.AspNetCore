# Model Binding

_Available starting with v1.9.0_

Composition handlers get access to the incoming `HttpRequest`. If the incoming request contains a body, the handler can access a body like the following:

```json
{
    "AString": "Some value"
}
```

By using the following code:

<!-- snippet: model-binding-raw-body-usage -->
<a id='snippet-model-binding-raw-body-usage'></a>
```cs
[HttpPost("/sample/{id}")]
public async Task Handle(HttpRequest request)
{
    request.Body.Position = 0;
    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true );
    var body = await reader.ReadToEndAsync();
    var content = JsonNode.Parse(body);

    //use the content object instance as needed
}
```
<sup><a href='/src/Snippets/ModelBinding/RawBodyUsageHandler.cs#L14-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-raw-body-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Something similar applies to getting data from the route, or from the query string, or the incoming form. For example:

<!-- snippet: model-binding-raw-route-data-usage -->
<a id='snippet-model-binding-raw-route-data-usage'></a>
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
<sup><a href='/src/Snippets/ModelBinding/RawBodyUsageHandler.cs#L30-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-raw-route-data-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Use model binding

It's possible to leverage ASP.Net built-in support for model binding to avoid accessing raw values of the incoming `HttpRequest`, such as the body.

To start using model binding, configure the ASP.Net application to add MVC components:

<!-- snippet: model-binding-add-controllers -->
<a id='snippet-model-binding-add-controllers'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddViewModelComposition();
    services.AddControllers();
}
```
<sup><a href='/src/Snippets/ModelBinding/ConfigureAppForModelBinding.cs#L8-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-add-controllers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> If, for reasons outside the scope of composing responses, there is the need to use MVC, Razor Pages, or just controllers and views, any MVC configuration options will add support for model binding.

The first step is to define the models, for example, in the above sample, the body model will look like the following C# class:

<!-- snippet: model-binding-model -->
<a id='snippet-model-binding-model'></a>
```cs
class BodyModel
{
    public string AString { get; set; }
}
```
<sup><a href='/src/Snippets/ModelBinding/BodyModel.cs#L3-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> The class name is irrelevant

Once we have a model for the body, a model representing the incoming request is needed. In a scenario in which there is the need to bind the body and the "id" from the route, the following request model can be used:

<!-- snippet: model-binding-request -->
<a id='snippet-model-binding-request'></a>
```cs
class RequestModel
{
    [FromRoute(Name = "id")] public int Id { get; set; }
    [FromBody] public BodyModel Body { get; set; }
}
```
<sup><a href='/src/Snippets/ModelBinding/RequestModel.cs#L5-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-request' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> The class name is irrelevant. The name of the properties marked as `[FromRoute]` or `[FromQueryString]` must match the route data names or query string key names. The name for the body, form, or property is irrelevant.

Once the models are defined, they can be used as follows:

<!-- snippet: model-binding-bind-body-and-route-data -->
<a id='snippet-model-binding-bind-body-and-route-data'></a>
```cs
[HttpPost("/sample/{id}")]
public async Task Handle(HttpRequest request)
{
    var requestModel = await request.Bind<RequestModel>();
    var body = requestModel.Body;
    var aString = body.AString;
    var id = requestModel.Id;

    //use values as needed
}
```
<sup><a href='/src/Snippets/ModelBinding/ModelBindingUsageHandler.cs#L10-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-bind-body-and-route-data' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For more information and options when using model binding, refer to the [Microsoft official documentation](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-5.0).

### TryBind to access the model state dictionary

_Available starting with v2.2.0_

The `TryBind` model binding option allows binding the incoming request and, at the same time, access additional information about the binding process:

<!-- snippet: model-binding-try-bind -->
<a id='snippet-model-binding-try-bind'></a>
```cs
[HttpPost("/sample/{id}")]
public async Task Handle(HttpRequest request)
{
    var (model, isModelSet, modelState) = await request.TryBind<RequestModel>();
    //use values as needed
}
```
<sup><a href='/src/Snippets/ModelBinding/ModelBindingUsageHandler.cs#L26-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-model-binding-try-bind' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `TryBind` return value is a tuple containing the binding result (the model), a boolean detailing if the model was set or not (helpful to distinguish between a model binder that does not find a value and the case where a model binder sets the `null` value), and the `ModelStateDictionary` to access binding errors.

## Declarative Model Binding

_Available starting with v4.1.0_

Instead of directly using the `Bind`/`TryBind` API to exercise the model binding engine, it is possible to use a set of `Bind*` attributes to declare the models the composition handlers want to bind. The following snippet demonstrates a composition handler that declares the binding to two models:

<!-- snippet: declarative-model-binding -->
<a id='snippet-declarative-model-binding'></a>
```cs
[HttpPost("/sample/{id}")]
[BindFromBody<BodyModel>]
[BindFromRoute<int>(routeValueKey: "id")]
public Task Handle(HttpRequest request)
{
    return Task.CompletedTask;
}
```
<sup><a href='/src/Snippets/ModelBinding/DeclarativeModelBinding.cs#L11-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-declarative-model-binding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The first model, of type `BodyModel`, a user type, is bound from the request body payload using the `BindFromBody<T>` attribute. The second model is an integer bound from the request route path using the "id" route value key.

The declarative model binding supports several binding sources through the following attributes:

- `BindFromBodyAttribute<T>`: Binds the given type T to the incoming request body payload.
- `BindFromRouteAttribute<T>`: Binds the given type T to the incoming route element identified by the `routeValueKey` argument.
- `BindFromQueryAttribute<T>`: Binds the given type T to the incoming query string parameter value identified by the `queryParameterName` argument.
- `BindFromFormAttribute<T>`: Binds the given type T to the incoming form fields collection. If the optional `formFieldName` is specified the binding operation only takes into account the specified form field as binding source; otherwise, the binding operation expects to bind to a `IFormCollection` type.
- `BindAttribute<T>`: Binds the given type T from multiple sources. Each `T` type property can specify the source using the various `FromBody`, `FromForm`, `FromRoute`, etc., default ASP.Net binding attributes.

Once model binding is declared, the bound models are accessible from the `ICompositionContext` arguments API, as demonstrated by the following snippet:

<!-- snippet: arguments-search-api -->
<a id='snippet-arguments-search-api'></a>
```cs
var ctx = request.GetCompositionContext();
var arguments = ctx.GetArguments(this);
var findValueByType = arguments.Argument<BodyModel>();
var findValueByTypeAndName = arguments.Argument<int>(name: "id");
var findValueByTypeAndSource = arguments.Argument<int>(bindingSource: BindingSource.Header);
var findValueByTypeSourceAndName = arguments.Argument<string>(name: "user", bindingSource: BindingSource.Query);
```
<sup><a href='/src/Snippets/ModelBinding/ArgumentsSearchAPI.cs#L13-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-arguments-search-api' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Arguments are segregated by component, to access them the arguments owner instance must be passed to the `GetArguments(owner)` method. Owners are restricted to a limited set of types: `ICompositionRequestsHandler`, `ICompositionEventsSubscriber`, and `ICompositionEventsHandler<T>`. 

### Named arguments experimental API

The API to search for arguments exposed by the composition context is experimental and, as such, subject to change. The `GetArguments(ownerComponent)` method is decorated with the `Experimental` attribute and will raise an `SC0001` warning. The warning can be suppressed with a regular pragma directive, e.g., `#pragma warning disable SC0001`.
