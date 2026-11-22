using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ComponentsProviders;

static class ComponentsProvidersSnippets
{
    static void RegisterReferencedComponents()
    {
        // begin-snippet: components-providers-register-referenced
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddViewModelComposition(options =>
        {
            options.AssemblyScanner.Disable();
            options.RegisterReferencedComponents();
        });
        // end-snippet
    }

    static void RegisterGeneratedProviders()
    {
        // begin-snippet: components-providers-register
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddViewModelComposition(options =>
        {
            options.AssemblyScanner.Disable();
            // One line per assembly containing composition components, e.g.
            // Sales.ViewModelComposition.GeneratedCompositionComponentsProvider
            // for the Sales.ViewModelComposition assembly
            options.RegisterComponentsFrom<Snippets.GeneratedCompositionComponentsProvider>();
        });
        // end-snippet
    }
}
