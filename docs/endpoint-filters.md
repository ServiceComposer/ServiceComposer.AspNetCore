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

## Reading a route's components

_Available starting with v6.0.0_

Endpoint filters and conventions see a composition endpoint, not the composition handlers it runs. Each composition endpoint carries a `CompositionEndpointComponents` metadata item listing the components of its route: request handlers, route-scoped subscribers, and the endpoint-scoped view model factory. For each, it gives its kind, the user's class and method, also for contract-less composition handlers, and their attributes, kept apart. Conventions can read it when the endpoints are built:

<!-- snippet: composition-endpoint-components -->
<a id='snippet-composition-endpoint-components'></a>
```cs
app.MapCompositionHandlers().Add(endpointBuilder =>
{
    var components = endpointBuilder.Metadata
        .OfType<CompositionEndpointComponents>()
        .Single()
        .Components;

    foreach (var component in components.Where(c => c.Kind == CompositionComponentKind.RequestsHandler))
    {
        // component.UserType and component.Method are the user's class and method,
        // also for contract-less composition handlers.
        Console.WriteLine($"{endpointBuilder.DisplayName}: {component.UserType.Name}.{component.Method.Name}, " +
                          $"{component.TypeAttributes.Count} class and {component.MethodAttributes.Count} method attributes");
    }
});
```
<sup><a href='/src/Snippets/EndpointFilters/ComponentsConvention.cs#L16-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-composition-endpoint-components' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The endpoint's own metadata merges the attributes of all its components, classes first, then methods.

