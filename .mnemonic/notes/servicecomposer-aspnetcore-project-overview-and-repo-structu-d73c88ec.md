---
title: ServiceComposer.AspNetCore — project overview and repo structure
tags:
  - servicecomposer
  - overview
  - architecture
  - repo-structure
  - viewmodel-composition
lifecycle: permanent
createdAt: '2026-03-12T19:42:04.591Z'
updatedAt: '2026-03-12T19:42:04.591Z'
project: https-github-com-servicecomposer-servicecomposer-aspnetcore
projectName: ServiceComposer.AspNetCore
memoryVersion: 1
---
## What is ServiceComposer?

ServiceComposer is a **ViewModel Composition Gateway** for ASP.NET Core. It solves the problem of displaying data owned by multiple autonomous services in SOA/microservices architectures without violating service boundaries, sharing databases, or creating distributed monoliths.

Each service contributes its own data to a shared dynamic `ExpandoObject` view model at the API gateway level. Handlers execute in parallel (scatter-gather), and the composed result is serialized back to the caller. This is the "read side" counterpart to NServiceBus messaging (the "write side").

**Author:** Mauro Servienti
**License:** Apache 2.0
**Target Framework:** .NET 8.0
**Current Major Version:** 4.x (MinVer minimum: 4.0)

## Repository Structure

```text
src/
  ServiceComposer.AspNetCore/                  # Main library
  ServiceComposer.AspNetCore.Tests/            # Integration tests (xUnit, .NET 8/9)
  ServiceComposer.AspNetCore.SourceGeneration/ # Incremental source generator (netstandard2.0)
  ServiceComposer.AspNetCore.SourceGeneration.Tests/
  TestClassLibraryWithHandlers/                # Test helper library
  Snippets/                                    # Documentation code snippets
docs/                                          # Markdown documentation
nugets/                                        # Package output directory
.github/workflows/                             # CI/CD (Windows + Linux, .NET 8/9)
```

The source generator ships inside the main NuGet package (in the `analyzers/dotnet/cs` folder).

## NuGet Dependencies

| Package | Range | Purpose |
| --- | --- | --- |
| `Microsoft.AspNetCore.App` | Framework ref | ASP.NET Core |
| `System.ValueTuple` | [4.5.0, 5.0.0) | Legacy tuple support |
| `Microsoft.Extensions.DependencyModel` | [8.0.0, 10.0.0) | Assembly scanning |
| `System.Reflection.Metadata` | [8.0.0, 10.0.0) | PE file validation |
| `System.Text.Json` | [8.0.5, 10.0.0) | JSON serialization (security update) |
| `MinVer` | 7.0.0 | Semantic versioning from git tags |
| `Microsoft.SourceLink.GitHub` | 8.0.0 | Source debugging |

## CI/CD

- GitHub Actions: Windows + Linux matrix
- .NET SDKs: 8.0.x and 9.0.x
- Versioning: MinVer from git tags (pattern `[0-9].[0-9]+.[0-9]`)
- Packages published to NuGet (releases) and Feedz.io (pre-releases)
