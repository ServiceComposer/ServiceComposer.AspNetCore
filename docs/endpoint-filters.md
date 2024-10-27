# Endpoint filters

_Available starting with v2.3.0_

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

For an endpoint filter to be included in te invocation pipeline, it must be registered at application configuration time:  

<!-- snippet: sample-endpoint-filter-registration -->
<a id='snippet-sample-endpoint-filter-registration'></a>
```cs
public void Configure(IApplicationBuilder app)
{
    app.UseEndpoints(builder =>
    {
        builder.MapCompositionHandlers()
            .AddEndpointFilter(new SampleEndpointFilter());
    });
}
```
<sup><a href='/src/Snippets/EndpointFilters/Startup.cs#L9-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-endpoint-filter-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
