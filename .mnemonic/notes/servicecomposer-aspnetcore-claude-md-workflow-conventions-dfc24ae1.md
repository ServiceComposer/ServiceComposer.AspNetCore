---
title: ServiceComposer.AspNetCore — CLAUDE.md workflow conventions
tags:
  - servicecomposer
  - workflow
  - snippets
  - mdsnippets
  - documentation
  - nuget
lifecycle: permanent
createdAt: '2026-03-12T19:43:51.543Z'
updatedAt: '2026-03-12T19:43:51.543Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
## Documentation Code Snippets Workflow

**Every code block added to a doc file MUST go through the Snippets project + mdsnippets.**

1. Create a `.cs` file under `src/Snippets/<Topic>/` with `// begin-snippet: name` / `// end-snippet` markers
2. Verify it compiles: `dotnet build src/Snippets/Snippets.csproj`
3. Place a `<!-- snippet: name -->` / `<!-- endSnippet -->` marker pair in the `.md` file (empty — no content between them)
4. Run `mdsnippets . -c InPlaceOverwrite` from the repo root to inject content and source links

Never write fenced code blocks directly in `.md` files for code that could be a compiled snippet.

## Snippets Project Notes

- Project: `src/Snippets/Snippets.csproj` — targets `net10.0`, `TreatWarningsAsErrors=true`
- All snippet source files use `using` + `namespace` declarations at top
- For WebApplicationBuilder-style setup snippets: wrap in `static void Method() { ... }` — top-level statements are not allowed in a library project
- `AssemblyScanner.AddAssemblyFilter` takes `Func<string, AssemblyScanner.FilterResults>` — not `Func<string, bool>`
- `IServiceProvider` requires `using System;`
- All snippet `.cs` files use `WebApplication.CreateBuilder()` pattern (not the old `Startup` class pattern)

## NuGet Package README

`NuGet.README.md` at the repo root is the package listing README shown on NuGet.org. It is **not** managed by mdsnippets — it uses plain fenced code blocks (no snippet markers) because the `<sup>` source link boilerplate injected by mdsnippets looks wrong on the package listing page.

When the Getting Started code examples change, update `NuGet.README.md` manually to keep it in sync with the root `README.md` and `docs/getting-started.md`.

The file is wired into the package via `<PackageReadmeFile>NuGet.README.md</PackageReadmeFile>` in `src/ServiceComposer.AspNetCore/ServiceComposer.AspNetCore.csproj`.
