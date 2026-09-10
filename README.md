# ApplicationBuilderHelpers

A .NET library for building command-line applications with a fluent API, dependency injection, and modular architecture.

- **Targets**: `net6.0`–`net10.0` · **AOT compatible** · **Trimmable**
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
| `Run` returns normally | `0` |
| `Run` throws `CommandException` | `ex.ExitCode` |
| Cancellation (`CancellationToken` / Ctrl+C) | `130` (128 + SIGINT) |

Return normally on success. Throw `CommandException` for errors to return a non-zero exit code from `RunAsync`:

```csharp
throw new CommandException("Operation failed", exitCode: 1);
```

See [Advanced Topics](docs/advanced.md) for more on sub-commands, custom host types, and error handling.

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
