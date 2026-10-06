# Starter Templates

Copy-only starter apps for ApplicationBuilderHelpers. Pick a variant, copy its folder, rename it, build it.

These are plain folders. There is no `template.json` and no `dotnet new` package. Copy the files with `cp` or your file manager.

## Pick a Variant

| Variant | Start here when you want | Layout |
|---|---|---|
| [Plain CLI](ApplicationBuilderHelpersTemplate/) (default) | A command-line app with clean architecture | `ApplicationBuilderHelpersTemplate.slnx`, 6 projects in `src/`, 2 test projects |
| [WebApi](ApplicationBuilderHelpersTemplate.WebApi/) | A REST API with auth and user management | `ApplicationBuilderHelpersTemplate.sln`, 13 projects in `src/`, 4 test projects |
| [WebApiAndWebApp](ApplicationBuilderHelpersTemplate.WebApiAndWebApp/) | A REST API plus a Blazor web app | `ApplicationBuilderHelpersTemplate.sln`, 22 projects in `src/`, 5 test projects |

New here? Start with Plain CLI. It is the smallest shape: `Presentation.Cli` references `Application`, `Infrastructure.InMemory`, and `Presentation` directly, and reaches `Domain` through `Application` (`src/Presentation.Cli/Presentation.Cli.csproj:17-19`).

## Copy a Template

Copy the whole folder to a new location outside this repo:

```bash
cp -r templates/ApplicationBuilderHelpersTemplate ~/MyApp
cd ~/MyApp
```

Use the matching folder name for the other variants (`ApplicationBuilderHelpersTemplate.WebApi` or `ApplicationBuilderHelpersTemplate.WebApiAndWebApp`).

## Rename Checklist

The copy still carries the template name. Work through this list before you build:

1. Rename the solution file (`ApplicationBuilderHelpersTemplate.slnx` or `ApplicationBuilderHelpersTemplate.sln`) to your app name.
2. Rename project namespaces (`Application.*`, `Domain.*`, `Infrastructure.*`, `Presentation.*`, plus `Domain.SourceGenerators` where present) to your app name.
3. Search for leftover `ApplicationBuilderHelpersTemplate` text (solution filenames, web template READMEs, Plain CLI agent guides) and `sampleapp` assembly names, then replace them.
4. Update `README.md`, `LICENSE.txt`, and company metadata in the copied `.csproj` files.

## Build and Run

```bash
dotnet build
dotnet run --project src/Presentation.Cli
```

The run command above is the Plain CLI entry point. For the web variants, run `.\build.ps1 init` (or `./build.sh init` on Linux/macOS) first to generate the secrets file, then run their presentation projects instead (see each template README).
