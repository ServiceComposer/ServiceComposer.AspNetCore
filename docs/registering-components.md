# Registering composition components

By default, ServiceComposer scans the assemblies in the application folder at startup and registers the composition components it finds: composition handlers, events subscribers and handlers, view model factories, preview handlers, and composition request filters. It also runs the [options customizations](options-customizations.md) it finds.

Assembly scanning relies on runtime reflection and on loading assemblies from disk. As an alternative, ServiceComposer generates at compile time a components provider for each assembly that contains composition components, and the host registers the providers with a single call.

## Generated components providers

The ServiceComposer.AspNetCore package includes a source generator. In every project referencing the package that contains composition components, the generator emits a public class named `GeneratedCompositionComponentsProvider`, in a namespace named after the assembly. For example, the `Sales.ViewModelComposition` assembly gets a `Sales.ViewModelComposition.GeneratedCompositionComponentsProvider` class.

The generated provider registers the same components that assembly scanning would find in the assembly, including `internal` ones and [contract-less composition handlers](contract-less-composition-requests-handlers.md), and runs the assembly's [options customizations](options-customizations.md).

## Registering all referenced components

In every project referencing the package, the generator also emits a `RegisterReferencedComponents()` extension method, in the `ServiceComposer.AspNetCore` namespace. It registers the project's own provider, if any, and the providers of all the assemblies the project references, directly or transitively:

<!-- snippet: components-providers-register-referenced -->
<a id='snippet-components-providers-register-referenced'></a>
```cs
var builder = WebApplication.CreateBuilder();
builder.Services.AddViewModelComposition(options =>
{
    options.AssemblyScanner.Disable();
    options.RegisterReferencedComponents();
});
```
<sup><a href='/src/Snippets/ComponentsProviders/ComponentsProvidersSnippets.cs#L11-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-components-providers-register-referenced' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The host is coupled only to the service assemblies it references: adding a composition component to a service requires no change to the host. The generated method is `internal`, so each project gets its own, listing the providers that project can see.

Referenced assemblies are found at compile time. Assemblies that are only copied to the application folder, without a reference, are not registered. Neither are assemblies compiled against a ServiceComposer version that doesn't generate providers.

## Registering providers one by one

To control which assemblies contribute composition components, register their providers explicitly:

<!-- snippet: components-providers-register -->
<a id='snippet-components-providers-register'></a>
```cs
var builder = WebApplication.CreateBuilder();
builder.Services.AddViewModelComposition(options =>
{
    options.AssemblyScanner.Disable();
    // One line per assembly containing composition components, e.g.
    // Sales.ViewModelComposition.GeneratedCompositionComponentsProvider
    // for the Sales.ViewModelComposition assembly
    options.RegisterComponentsFrom<Snippets.GeneratedCompositionComponentsProvider>();
});
```
<sup><a href='/src/Snippets/ComponentsProviders/ComponentsProvidersSnippets.cs#L23-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-components-providers-register' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Customizations from all registered providers run before any component is registered. `RegisterReferencedComponents` and `RegisterComponentsFrom` must be called from the `AddViewModelComposition` configuration callback. Calling them from an options customization or a components provider throws an `InvalidOperationException`. Registering the same provider more than once has no effect, so the two approaches can be combined.

> [!NOTE]
> The host project gets its own generated provider if it contains composition components. If the host code is in a namespace that also contains a `GeneratedCompositionComponentsProvider` class, use the fully qualified name of the provider to register.

### What isn't registered

A generated provider can only register types it can reference. The following types are skipped:

- Types that aren't accessible at the assembly level, such as `private` or `protected` nested classes.
- Abstract classes and open generic types.
- Options customizations without a public or internal parameterless constructor.

Register these types manually, or make them accessible.

## Combining providers and assembly scanning

Generated providers and assembly scanning can be used together, for example while migrating. When assembly scanning is enabled, assemblies with a registered components provider are not scanned, so their components are registered only once.
