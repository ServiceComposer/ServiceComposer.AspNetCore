# Response serialization casing

ServiceComposer serializes responses using a JSON serializer. By default responses are serialized using camel casing, a C# `SampleProperty` property is serialized as `sampleProperty`. Consumers can influence the response casing of a specific request by adding to the request an `Accept-Casing` custom HTTP header. Accepted values are `casing/camel` (default) and `casing/pascal`.

## Default response serialization casing

_Available starting with v1.8.0_

Default response serialization is camel casing. It's possible to configure a different default response serialization casing when configuring ServiceComposer:

<!-- snippet: default-casing -->
<a id='snippet-default-casing'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddRouting();
    services.AddViewModelComposition(options =>
    {
        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
    });
}
```
<sup><a href='/src/Snippets/DefaultCasing/Startup.cs#L8-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-default-casing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Requests containing the `Accept-Casing` custom HTTP header will still be honored.
