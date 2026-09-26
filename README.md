# ApplicationBuilderHelpers

A .NET library for building command-line applications with a fluent API, dependency injection, and modular architecture.

- **Targets**: `net6.0`–`net10.0` · **AOT-compatible** · **Trimmable**
- **Dependencies**: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `AbsolutePathHelpers`

## Features

- 🎯 **Command-based Architecture** — Command patterns with automatic argument parsing
- 🔧 **Fluent Builder API** — Intuitive setup via method chaining
- 💉 **Dependency Injection** — Full `Microsoft.Extensions.DependencyInjection` support
- 🏗️ **Modular Application Structure** — Reusable `ApplicationDependency` modules with lifecycle hooks
- ⚙️ **Configuration** — .NET configuration integration with `@ref:` reference values
- 🎨 **Attributes** — `[Command]`, `[CommandOption]`, `[CommandArgument]` for declarative CLI definitions
- 🎯 **Sub-Commands** — Hierarchical commands via space-separated names
- 🖌️ **Themable Help** — 5 built-in console color themes, configurable help width
- 🧩 **Multiple Host Types** — `HostApplicationBuilder`, `WebApplicationBuilder`, custom builders

## Installation

```bash
dotnet add package ApplicationBuilderHelpers
```

## Quick Start

```csharp
// Program.cs
using ApplicationBuilderHelpers;

return await ApplicationBuilder.Create()
    .AddApplication<CoreApplication>()
    .AddCommand<GreetCommand>()
    .RunAsync(args);
```

```csharp
[Command(description: "Greet someone")]
public class GreetCommand : Command
{
    [CommandArgument(Name = "name", Position = 0, Description = "Who to greet")]
    public string Name { get; set; } = "World";

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}
```

```bash
$ myapp Alice
Hello, Alice!
```

A near-miss of a subcommand name still exits `2`. Use `--` to force positional binding (`myapp -- Alice`).

## Core Concepts

### Commands

Extend `Command` and override `Run`. Define options with `[CommandOption]` and positional arguments with `[CommandArgument]`. Commands can register their own services, middleware, and configuration — they inherit the full `ApplicationDependency` lifecycle.

```csharp
[Command("build", description: "Build the project")]
public class BuildCommand : Command
{
    [CommandOption('v', "verbose", Description = "Enable verbose output")]
    public bool Verbose { get; set; }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        // ...build logic...
    }
}
```

### ApplicationDependency

Group shared services and configuration into reusable modules:

```csharp
public class CoreApplication : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder appBuilder, IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
    }
}
```

See [Application Dependencies](docs/application-dependencies.md) for the full lifecycle reference.

### Sub-Commands

Use space-separated names for hierarchical commands. Try `myapp deploy prod` or `myapp deploy prod rollback`:

```csharp
[Command("deploy prod", description: "Deploy to production")]
public class DeployProductionCommand : Command { /* ... */ }
```

### Exit Codes

`RunAsync` returns an exit code:

| Outcome | Exit code |
|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` (conversion failure beats help-with-values; invalid+version still `0` via the pre-validation version guard at `CommandLineParser.cs:78-82`; leading `--help`/`-h` on a concrete root renders global help, exit `0`, before trailing validation — `IsConcreteRootLeadingHelp` at `ArgumentParser.cs:530-537`, pinned by `RootRoutingDivergenceTests.cs`) |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` is reserved and never thrown — valued repeats resolve last-wins) | `2` |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` (custom host-code passthrough preserved) |
| Cancellation (`CancellationToken` / Ctrl+C) | `130` (128 + SIGINT) |

Bare root (no root implementation, only leaf subcommands): `myapp` with zero args exits `2` with `'<root>' requires a subcommand. Available subcommands: ...` plus the two-sentence global usage footer (`SubCommandInfo.cs:32`; `ArgumentParser.cs:76-96`; `CommandErrorFooter.cs:21-58`). Help-first (`myapp --help greet`) renders global help, exit `0` (`ArgumentParser.cs:47-62`; `HelpFormatter.cs:40-42`) — and on a concrete root (a description-only `[Command]` merged at root, `HasImplementation` true) a leading bare `--help`/`-h` renders global help before trailing validation, exit `0` (`IsConcreteRootLeadingHelp` at `ArgumentParser.cs:530-537`; version still beats help) — see [Advanced Topics](docs/advanced.md#bare-root-and-help-first).

Return normally on success. Throw `CommandException` for errors to return a non-zero exit code from `RunAsync`:

```csharp
throw new CommandException("Operation failed", exitCode: 1);
```

Shell completion (`complete` / `completions ...`) resolves through the `CompletionGateway` pre-parse stage first — see [Commands](docs/commands.md#shell-completion) for the consolidated 0/1/2 exit matrix.

See [Advanced Topics](docs/advanced.md) for more on sub-commands, custom host types, error handling, and error footers. Every help screen (global and per-command) lists `-V, --version` under `GLOBAL OPTIONS:`; usage-error footers hint at both `--help` and `--version`, while `Fault`/`NoImplementation` keep the single-sentence `--help`-only footer.

## Architecture

```
┌─────────────────────┐
│  ApplicationBuilder │ ← Entry Point (fluent API)
└──────────┬──────────┘
           │
    ┌──────▼──────┐
    │  Commands   │ ← Command Registration (+ own lifecycle hooks)
    └──────┬──────┘
           │
    ┌──────▼──────────┐
    │  Applications   │ ← Application Modules (lifecycle hooks)
    └──────┬──────────┘
           │
    ┌──────▼───────────┐
    │  Host Builder    │ ← Host Configuration
    └──────┬───────────┘
           │
    ┌──────▼──────┐
    │  Services   │ ← Dependency Injection
    └──────┬──────┘
           │
    ┌──────▼──────────┐
    │  Middleware     │ ← Request Pipeline
    └──────┬──────────┘
           │
    ┌──────▼──────┐
    │  Execution  │ ← Command Execution
    └─────────────┘
```

`RunAsync` pipeline stages: hierarchy build → `CompletionGateway` (completion > help > parse > version) → help → parse → version check → execute.

## Documentation

| Guide | |
|---|---|
| [Getting Started](docs/getting-started.md) | Installation, first app, services |
| [Commands](docs/commands.md) | Attributes, options, arguments, lifecycle |
| [Application Dependencies](docs/application-dependencies.md) | Full lifecycle reference |
| [Configuration & Themes](docs/configuration.md) | Fluent config, themes, `@ref:` system, help formatting |
| [Custom Type Parsers](docs/custom-type-parsers.md) | `ICommandTypeParser` / `CommandTypeParser<T>` |
| [Advanced Topics](docs/advanced.md) | Sub-commands, host types, exit codes, error handling |
| [API Reference](docs/api-reference.md) | Complete public API surface |

## Contributing

Contributions are welcome! Please submit a Pull Request.

## License

MIT — see the [LICENSE](LICENSE) file.
