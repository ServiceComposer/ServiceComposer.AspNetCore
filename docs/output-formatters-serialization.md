# Output formatters

_Available starting with v1.9.0_

Enabling output formatters support is a matter of:

<!-- snippet: use-output-formatters -->
<a id='snippet-use-output-formatters'></a>
```cs
builder.Services.AddViewModelComposition(options =>
{
    options.ResponseSerialization.UseOutputFormatters = true;
});
builder.Services.AddControllers();
```
<sup><a href='/src/Snippets/Serialization/UseOutputFormatters.cs#L13-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-use-output-formatters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The required steps are:

- set `UseOutputFormatters` to `true`
- Set up the MVC components by calling one of the following:
  - `AddControllers()`
  - `AddControllersWithViews()`
  - `AddMvc()`
  - `AddRazorPages()`

Starting with v6.0.0, if output formatters are enabled and the MVC components aren't set up, mapping the composition handlers throws an `InvalidOperationException` at startup. Before, the first request failed. The same applies to scatter-gather routes with `UseOutputFormatters` enabled.

The above configuration uses the new `System.Text.Json` serializer as the default json serializer to format json responses. It's possible to plug-in the Newtonsoft Json.Net serializer output formatter by adding a package reference to the `Microsoft.AspNetCore.Mvc.NewtonsoftJson` package, and using the following configuration:

<!-- snippet: use-newtonsoft-output-formatters -->
<a id='snippet-use-newtonsoft-output-formatters'></a>
```cs
builder.Services.AddViewModelComposition(options =>
{
    options.ResponseSerialization.UseOutputFormatters = true;
});
builder.Services.AddControllers()
    .AddNewtonsoftJson();
```
<sup><a href='/src/Snippets/Serialization/UseOutputFormatters.cs#L26-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-use-newtonsoft-output-formatters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Response casing

_Available starting with v1.9.0_

With output formatters, MVC serializes the responses, so the casing is controlled by the MVC serializer configuration, not by ServiceComposer:

- The `ResponseSerialization.DefaultResponseCasing` option and the [custom JSON serializer settings](custom-json-response-serialization-settings.md) are ignored, and a warning is logged at startup if they're configured.
- The `Accept-Casing` request header is ignored.

> [!IMPORTANT]
> The default `dynamic` view model is serialized as a dictionary, and MVC's default JSON configuration only camel cases properties, not dictionary keys. With output formatters, strongly typed view models are camel cased by default, but the members of the `dynamic` view model aren't: `vm.SampleValue` is serialized as `SampleValue`. To camel case them too, configure the dictionary keys casing as shown below.

### System.Text.Json

To camel case the members of the `dynamic` view model, as the properties of strongly typed view models, set the `DictionaryKeyPolicy`:

<!-- snippet: output-formatters-system-text-json-camel-case -->
<a id='snippet-output-formatters-system-text-json-camel-case'></a>
```cs
builder.Services.AddViewModelComposition(options =>
{
    options.ResponseSerialization.UseOutputFormatters = true;
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Properties are camel cased by default, the dynamic view model's members aren't
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });
```
<sup><a href='/src/Snippets/Serialization/OutputFormattersCasing.cs#L15-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-output-formatters-system-text-json-camel-case' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To use pascal casing, remove the naming policy:

<!-- snippet: output-formatters-system-text-json-pascal-case -->
<a id='snippet-output-formatters-system-text-json-pascal-case'></a>
```cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
```
<sup><a href='/src/Snippets/Serialization/OutputFormattersCasing.cs#L33-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-output-formatters-system-text-json-pascal-case' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Newtonsoft Json.NET

To camel case the members of the `dynamic` view model, as the properties of strongly typed view models, process dictionary keys:

<!-- snippet: output-formatters-newtonsoft-camel-case -->
<a id='snippet-output-formatters-newtonsoft-camel-case'></a>
```cs
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            // Also camel case the dynamic view model's members
            NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true }
        };
    });
```
<sup><a href='/src/Snippets/Serialization/OutputFormattersCasing.cs#L46-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-output-formatters-newtonsoft-camel-case' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To use pascal casing, use the default contract resolver:

<!-- snippet: output-formatters-newtonsoft-pascal-case -->
<a id='snippet-output-formatters-newtonsoft-pascal-case'></a>
```cs
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    });
```
<sup><a href='/src/Snippets/Serialization/OutputFormattersCasing.cs#L63-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-output-formatters-newtonsoft-pascal-case' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
