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

snippet: net-core-3x-model-binding-model

snippet: net-core-3x-model-binding-request

snippet: net-core-3x-model-binding-bind-body-and-route-data
