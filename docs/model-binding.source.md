# Model Binding

_Available starting with v1.9.0_

Composition handlers get access to the incoming `HttpRequest`. If the incoming request contains a body, the handler can access a body like the following:

```json
{
    "AString": "Some value"
}
```

by using the following code:

snippet: net-core-3x-model-binding-raw-body-usage

Something similar applies to getting data from the route, or from the query string, or the incoming form. For example:

snippet: net-core-3x-model-binding-raw-route-data-usage

## Use model binding

It's possible to leverage ASP.Net built-in support for model binding to avoid accessing raw values of the incoming `HttpRequest`, such as the body.

To start using model binding configure the ASP.Net application to add MVC components:

snippet: net-core-3x-model-binding-add-controllers

> If, for reasons outside the scope of composing responses, there is the need to use MVC, or Razor Pages, or just controllers and views, any of the MVC configuration options will add support for model binding.

THe first step is to define the models, for example in the above sample the body model will look like the following C# class:

snippet: net-core-3x-model-binding-model

> The class name is irrelevant

Once we have a model for the body, a model that represent the incoming request is needed. In in a scenario in which there is the need to bind the body and the id from the route, the following request model can be used:

snippet: net-core-3x-model-binding-request

> The class name is irrelevant. The name of the properties marked as `[FromRoute]` or `[FromQueryString]` must match the route data names or query string keys names. The name for the body, or form, property is irrelevant.

Once the models are defined they can be used as follows:

snippet: net-core-3x-model-binding-bind-body-and-route-data

For more information and options when using model binding refer to the [Microsoft official documentation](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-5.0).
