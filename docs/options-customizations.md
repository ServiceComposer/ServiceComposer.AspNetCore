# Customizing ViewModel Composition options from dependent assemblies

Assemblies containing types participating in the composition process can customize the current application `ViewModelCompositionOptions` by defining a type that implements the `IViewModelCompositionOptionsCustomization` interface. At runtime, when the application starts, types implementing the `IViewModelCompositionOptionsCustomization` will be instantiated and the `Customize` method will be invoked.

> [!NOTE]
> Types implementing `IViewModelCompositionOptionsCustomization` are not managed by IoC container. Dependency injection is not available.

This pattern lets a library or plugin assembly apply its own composition configuration without requiring changes to the host application's startup code:

<!-- snippet: options-customization-basic -->
<a id='snippet-options-customization-basic'></a>
```cs
// In a class library assembly (e.g. Sales.ViewModelComposition.dll)
public class SalesCompositionOptionsCustomization : IViewModelCompositionOptionsCustomization
{
    public void Customize(ViewModelCompositionOptions options)
    {
        options.AssemblyScanner.AddAssemblyFilter(name =>
            name.StartsWith("Sales.")
                ? AssemblyScanner.FilterResults.Include
                : AssemblyScanner.FilterResults.Exclude);
    }
}
```
<sup><a href='/src/Snippets/OptionsCustomizations/OptionsCustomizationSnippets.cs#L8-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-options-customization-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When the host application calls `builder.Services.AddViewModelComposition()`, ServiceComposer scans loaded assemblies and automatically discovers and invokes all `IViewModelCompositionOptionsCustomization` implementations.

## Accessing configuration

`ViewModelCompositionOptions` offers the ability to access the application `IConfiguration` instance. By default, the `ViewModelCompositionOptions.Configuration` property is null — accessing it throws an `ArgumentException`. To enable configuration support, pass the `IConfiguration` instance when configuring ServiceComposer:

<!-- snippet: options-customization-with-configuration -->
<a id='snippet-options-customization-with-configuration'></a>
```cs
var builder = WebApplication.CreateBuilder();
builder.Services.AddViewModelComposition(builder.Configuration);
```
<sup><a href='/src/Snippets/OptionsCustomizations/OptionsCustomizationSnippets.cs#L26-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-options-customization-with-configuration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

With that, a customization class can read configuration values:

<!-- snippet: options-customization-read-configuration -->
<a id='snippet-options-customization-read-configuration'></a>
```cs
public class SalesCompositionOptionsCustomizationWithConfig : IViewModelCompositionOptionsCustomization
{
    public void Customize(ViewModelCompositionOptions options)
    {
        var section = options.Configuration.GetSection("Sales:Composition");
        // use section values to conditionally configure options
    }
}
```
<sup><a href='/src/Snippets/OptionsCustomizations/OptionsCustomizationSnippets.cs#L33-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-options-customization-read-configuration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
