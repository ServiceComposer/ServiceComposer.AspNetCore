# Custom JSON response serialization settings

_Available starting with v1.8.0_

By default each response is serialized using [Json.Net](https://www.newtonsoft.com/json/help/html/Introduction.htm) and serialization settings (`JsonSerializerSettings`) are determined by the [requested response casing](response-serialization-casing.source.md). If the requested casing is camel casing, the default, the folowing serialization settings are applied to the response:

<!-- snippet: net-core-3x-camel-serialization-settings -->
<a id='snippet-net-core-3x-camel-serialization-settings'></a>
```cs
var settings = new JsonSerializerSettings()
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};
```
<sup><a href='/src/Snippets.NetCore3x/Serialization/ResponseSettingsBasedOnCasing.cs#L10-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-camel-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the requested case is pascal, the following settings are applied:

<!-- snippet: net-core-3x-pascal-serialization-settings -->
<a id='snippet-net-core-3x-pascal-serialization-settings'></a>
```cs
var settings = new JsonSerializerSettings();
```
<sup><a href='/src/Snippets.NetCore3x/Serialization/ResponseSettingsBasedOnCasing.cs#L20-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-pascal-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

It's possible to customize the response serialization settings on a case-by-case using the following configuration:

<!-- snippet: net-core-3x-custom-serialization-settings -->
<a id='snippet-net-core-3x-custom-serialization-settings'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddRouting();
    services.AddViewModelComposition(options =>
    {
        options.ResponseSerialization.UseCustomJsonSerializerSettings(request =>
        {
            return new JsonSerializerSettings();
        });
    });
}
```
<sup><a href='/src/Snippets.NetCore3x/Serialization/Startup.cs#L9-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-custom-serialization-settings' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each time ServiceComposer needs to serialize a response it'll invoke the supplied function.

NOTE:
When customizing the serialization settings, it's responsibility of the function to configure the correct resolver for the requested casing
