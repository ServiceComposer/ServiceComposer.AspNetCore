using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.OptionsCustomizations;

// begin-snippet: options-customization-basic
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
// end-snippet

static class OptionsCustomizationConfigSnippets
{
    static void ShowConfigurationSetup()
    {
        // begin-snippet: options-customization-with-configuration
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddViewModelComposition(builder.Configuration);
        // end-snippet
    }
}

// begin-snippet: options-customization-read-configuration
public class SalesCompositionOptionsCustomizationWithConfig : IViewModelCompositionOptionsCustomization
{
    public void Customize(ViewModelCompositionOptions options)
    {
        var section = options.Configuration.GetSection("Sales:Composition");
        // use section values to conditionally configure options
    }
}
// end-snippet
