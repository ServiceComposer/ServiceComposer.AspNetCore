# ServiceComposer.AspNetCore — Claude Instructions

## Snippets project notes

- Project: `src/Snippets/Snippets.csproj` — targets `net10.0`, `TreatWarningsAsErrors=true`
- All snippet source files use `using` + `namespace` declarations at top
- For WebApplicationBuilder-style setup snippets: wrap in `static void Method() { ... }` — top-level statements are not allowed in a library project
- `AssemblyScanner.AddAssemblyFilter` takes `Func<string, AssemblyScanner.FilterResults>` — not `Func<string, bool>`
- `IServiceProvider` requires `using System;`
- All snippet `.cs` files use `WebApplication.CreateBuilder()` pattern (not the old `Startup` class pattern)

## Project context

- **What**: ViewModel Composition Gateway for ASP.NET Core — parallel scatter-gather of handler outputs into a shared dynamic view model
- **Docs location**: `docs/` — managed by MarkdownSnippets (`run-markdownsnippets.yml` workflow)
- **Key interfaces**: `ICompositionRequestsHandler`, `ICompositionEventsSubscriber`, `ICompositionEventsHandler<T>`, `IViewModelFactory`, `IEndpointScopedViewModelFactory`

## NuGet package README

`NuGet.README.md` at the repo root is the package listing README shown on NuGet.org. It is **not** managed by mdsnippets — it uses plain fenced code blocks (no snippet markers) because the `<sup>` source link boilerplate injected by mdsnippets looks wrong on the package listing page.

When the Getting Started code examples change, update `NuGet.README.md` manually to keep it in sync with the root `README.md` and `docs/getting-started.md`.

The file is wired into the package via `<PackageReadmeFile>NuGet.README.md</PackageReadmeFile>` in `src/ServiceComposer.AspNetCore/ServiceComposer.AspNetCore.csproj`.

## Architecture reference

For detailed architecture, retrieve mnemonic memory
