# Custom JSON response serialization settings

_Available starting with v1.8.0_

By default, each response is serialized using [Json.Net](https://www.newtonsoft.com/json/help/html/Introduction.htm) and serialization settings (`JsonSerializerSettings`) are determined by the [requested response casing](response-serialization-casing.source.md). If the requested casing is camel casing, the default, the folowing serialization settings are applied to the response:

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

If the requested case is pascal, the following settings are applied:

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
public void ConfigureServices(IServiceCollection services)
{
    services.AddRouting();
    services.AddViewModelComposition(options =>
    {
        options.ResponseSerialization.UseCustomJsonSerializerSettings(_ =>
        {
            return new JsonSerializerOptions()
            {
                // customize options as needed
            };
        });
    });
}
```
<sup><a href='/src/Snippets/Serialization/Startup.cs#L9-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-custom-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each time ServiceComposer needs to serialize a response it'll invoke the supplied function.

> [!NOTE]
> When customizing the serialization settings, it's responsibility of the function to configure the correct resolver for the requested casing
