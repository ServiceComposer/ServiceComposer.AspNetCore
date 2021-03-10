# Output formatters

_Available starting with v1.9.0_

Enabling output formatters support is a metter of:

snippet: net-core-3x-use-output-formatters

The required steps are:

- set `UseOutputFormatters` to `true`
- Set up the MVC components by calling one of the following:
  - `AddControllers()`
  - `AddControllersAndViews()`
  - `AddMvc`
  - `AddRazorPages()`

The above configuration uses the new `System.Text.Json` serializer as the default json serializer to format json responses. The `System.Text.Json` serializer does not support serializing `dynamic` objects. It's possible to plug-in the Newtonsoft Json.Net serializer as output formatter by adding a package reference to the `Microsoft.AspNetCore.Mvc.NewtonsoftJson` package, and using the following configuration:

snippet: net-core-3x-use-newtonsoft-output-formatters
