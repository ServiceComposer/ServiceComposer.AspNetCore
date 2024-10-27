# Output formatters

_Available starting with v1.9.0_

Enabling output formatters support is a matter of:

<!-- snippet: use-output-formatters -->
<a id='snippet-use-output-formatters'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddViewModelComposition(options =>
    {
        options.ResponseSerialization.UseOutputFormatters = true;
    });
    services.AddControllers();
}
```
<sup><a href='/src/Snippets/Serialization/UseOutputFormatters.cs#L8-L17' title='Snippet source file'>snippet source</a> | <a href='#snippet-use-output-formatters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The required steps are:

- set `UseOutputFormatters` to `true`
- Set up the MVC components by calling one of the following:
  - `AddControllers()`
  - `AddControllersAndViews()`
  - `AddMvc`
  - `AddRazorPages()`

The above configuration uses the new `System.Text.Json` serializer as the default json serializer to format json responses. The `System.Text.Json` serializer does not support serializing `dynamic` objects. It's possible to plug-in the Newtonsoft Json.Net serializer as output formatter by adding a package reference to the `Microsoft.AspNetCore.Mvc.NewtonsoftJson` package, and using the following configuration:

<!-- snippet: use-newtonsoft-output-formatters -->
<a id='snippet-use-newtonsoft-output-formatters'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddViewModelComposition(options =>
    {
        options.ResponseSerialization.UseOutputFormatters = true;
    });
    services.AddControllers()
        .AddNewtonsoftJson();
}
```
<sup><a href='/src/Snippets/Serialization/UseOutputFormatters.cs#L22-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-use-newtonsoft-output-formatters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
