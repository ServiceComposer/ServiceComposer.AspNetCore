# Custom JSON response serialization settings

_Available starting with v1.8.0_

By default, each response is serialized using `System.Text.Json`. Starting with v6.0.0, the serialization settings (`JsonSerializerOptions`) are the [minimal APIs JSON options](response-serialization-casing.md#minimal-apis-json-options), configured with `ConfigureHttpJsonOptions()`. When a [response casing](response-serialization-casing.md) is requested, or set explicitly as the default, it replaces their naming policies. For camel casing, the naming policies are:

<!-- snippet: camel-serialization-settings -->
<a id='snippet-camel-serialization-settings'></a>
```cs
var settings = new JsonSerializerOptions()
{
    // System.Text.Json requires both properties to be
    // set to properly format serialized responses
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
};
```
<sup><a href='/src/Snippets/Serialization/ResponseSettingsBasedOnCasing.cs#L9-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-camel-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For pascal casing, there are no naming policies, as in:

<!-- snippet: pascal-serialization-settings -->
<a id='snippet-pascal-serialization-settings'></a>
```cs
var settings = new JsonSerializerOptions();
```
<sup><a href='/src/Snippets/Serialization/ResponseSettingsBasedOnCasing.cs#L22-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-pascal-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

It's possible to customize the response serialization settings on a case-by-case using the following configuration:

<!-- snippet: custom-serialization-settings -->
<a id='snippet-custom-serialization-settings'></a>
```cs
builder.Services.AddRouting();
builder.Services.AddViewModelComposition(options =>
{
    options.ResponseSerialization.UseCustomJsonSerializerSettings(_ =>
    {
        return new JsonSerializerOptions()
        {
            // customize options as needed
        };
    });
});
```
<sup><a href='/src/Snippets/Serialization/Startup.cs#L14-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-custom-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each time ServiceComposer needs to serialize a response it'll invoke the supplied function. The returned settings replace the minimal APIs JSON options entirely.

> [!NOTE]
> When customizing the serialization settings, it's responsibility of the function to configure the correct resolver for the requested casing
