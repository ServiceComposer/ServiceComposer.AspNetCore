# Response serialization casing

ServiceComposer serializes responses using a JSON serializer. By default, responses are serialized using camel casing, a C# `SampleProperty` property is serialized as `sampleProperty`. Consumers can influence the response casing of a specific request by adding to the request an `Accept-Casing` custom HTTP header. Accepted values are `casing/camel` (default) and `casing/pascal`.

## Minimal APIs JSON options

_Available starting with v6.0.0_

When [output formatters](output-formatters-serialization.md) aren't used, the default, responses are serialized with the minimal APIs JSON options, the same ones minimal API endpoints use. They're configured with `ConfigureHttpJsonOptions()`:

<!-- snippet: minimal-apis-json-options -->
<a id='snippet-minimal-apis-json-options'></a>
```cs
builder.Services.AddViewModelComposition();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```
<sup><a href='/src/Snippets/Serialization/MinimalApisJsonOptions.cs#L15-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-minimal-apis-json-options' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The view model members follow the configured `PropertyNamingPolicy`, also when the view model is the default `dynamic` one: since it's serialized as a dictionary, its members use the `DictionaryKeyPolicy` if one is configured, and the `PropertyNamingPolicy` otherwise. Without any configuration, the minimal APIs JSON options use camel casing.

> [!NOTE]
> Settings that System.Text.Json only applies to properties, such as `DefaultIgnoreCondition`, don't apply to the members of the default `dynamic` view model. Use a [view model factory](view-model-factory.md) to create a typed view model if needed.

A default response casing set explicitly, or the `Accept-Casing` header, replaces only the naming policies: the other minimal APIs JSON options, e.g. converters, still apply. [Custom serialization settings](custom-json-response-serialization-settings.md) replace the minimal APIs JSON options entirely.

## Default response serialization casing

_Available starting with v1.8.0_

Default response serialization casing is camel case. It's possible to configure a different default response serialization casing when configuring ServiceComposer. Starting with v6.0.0, it replaces the naming policies of the minimal APIs JSON options:

<!-- snippet: default-casing -->
<a id='snippet-default-casing'></a>
```cs
builder.Services.AddRouting();
builder.Services.AddViewModelComposition(options =>
{
    options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
});
```
<sup><a href='/src/Snippets/DefaultCasing/Startup.cs#L13-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-default-casing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Requests containing the `Accept-Casing` custom HTTP header will still be honored.
