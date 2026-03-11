# Endpoint filters

_Available starting with v3.0.0_

Endpoint filters allow intercepting all incoming HTTP requests prior to invoking the composition pipeline.

## Defining endpoint filters

Defining an endpoint filter requires defining a class that implements the `IEndpointFilter` interface, like in the following snippet:

<!-- snippet: sample-endpoint-filter -->
<a id='snippet-sample-endpoint-filter'></a>
```cs
class SampleEndpointFilter : IEndpointFilter
{
    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Do something meaningful prior to invoking the rest of the pipeline
        
        var response = await next(context);

        // Do something meaningful with the response

        return response;
    }
}
```
<sup><a href='/src/Snippets/EndpointFilters/SampleEndpointFilter.cs#L6-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-endpoint-filter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Registering endpoint filters

For an endpoint filter to be included in the invocation pipeline, it must be registered at application configuration time:

<!-- snippet: sample-endpoint-filter-registration -->
<a id='snippet-sample-endpoint-filter-registration'></a>
```cs
app.MapCompositionHandlers()
    .AddEndpointFilter(new SampleEndpointFilter());
```
<sup><a href='/src/Snippets/EndpointFilters/Startup.cs#L14-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-endpoint-filter-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Accessing arguments

The endpoint filters API exposes through the `EndpointFilterInvocationContext` the list of arguments that the ASP.NET model binding engine determined as needed by the later invoked controller action. When using regular composition handlers, e.g. by implementing the `ICompositionRequestsHandler` interface, ServiceComposer cannot determine which arguments are later needed by the user composition code. To overcome this limitation and allow the arguments list to be populated and accessible by filters, it's required to use a [declarative model binding approach](model-binding.md#declarative-model-binding).

For details on the arguments search API, see [Named arguments experimental API](model-binding.md#named-arguments-experimental-api).
