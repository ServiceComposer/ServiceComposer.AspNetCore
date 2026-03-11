# ServiceComposer.AspNetCore — Claude Instructions

## Documentation code snippets workflow

**Every code block added to a doc file MUST go through the Snippets project + mdsnippets.**

1. Create a `.cs` file under `src/Snippets/<Topic>/` with `// begin-snippet: name` / `// end-snippet` markers
2. Verify it compiles: `dotnet build src/Snippets/Snippets.csproj`
3. Place a `<!-- snippet: name -->` / `<!-- endSnippet -->` marker pair in the `.md` file (empty — no content between them)
4. Run `mdsnippets . -c InPlaceOverwrite` from the repo root to inject content and source links

Never write fenced code blocks directly in `.md` files for code that could be a compiled snippet.

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

## Architecture reference

For detailed architecture, interfaces, pipeline flow, and source file map see [`context/context.md`](context/context.md).
