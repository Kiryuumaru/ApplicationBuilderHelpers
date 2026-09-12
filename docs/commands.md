# Commands

Commands are the core unit of work in ApplicationBuilderHelpers. Each command is a class that processes a specific CLI operation.

## Defining a Command

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

[Command("build", description: "Build the project")]
public class BuildCommand : Command
{
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        // Command logic here. A normal return means success (exit 0).
    }
}
```

### Command Attribute

`[Command]` supports these constructors:

```csharp
[Command(description: "Description only")]          // Auto-detects name from class
[Command("name")]                                     // Name only
[Command("name", description: "With description")]    // Both
```

The `Term` property is the command name. Use space-separated names for sub-commands:

```csharp
[Command("deploy prod", description: "Deploy to production")]
```

### Command Variants

| Base Class | Host Builder | Use For |
|---|---|---|
| `Command` | `HostApplicationBuilder` | Console apps, workers |
| `Command<THostApplicationBuilder>` | Custom | Web apps, custom hosts |

## Options

Define command-line flags with `[CommandOption]`:

```csharp
[CommandOption('v', "verbose", Description = "Enable verbose output")]
public bool Verbose { get; set; }

[CommandOption('c', "config", Description = "Config file path", EnvironmentVariable = "APP_CONFIG")]
public string? ConfigPath { get; set; }

[CommandOption("timeout", Description = "Timeout in seconds")]
public int Timeout { get; set; } = 30;
```

### Option Constructors

```csharp
[CommandOption('s', "long-name")]   // Short + long
[CommandOption('s')]                 // Short only
[CommandOption("long-name")]         // Long only
```

### Option Properties

| Property | Type | Description |
|---|---|---|
| `Term` | `string?` | Long option name (e.g., `"verbose"` → `--verbose`) |
| `ShortTerm` | `char?` | Short option flag (e.g., `'v'` → `-v`) |
| `Description` | `string?` | Help text |
| `EnvironmentVariable` | `string?` | Fallback env var |
| `Required` | `bool` | Must be provided |
| `FromAmong` | `object[]` | Restrict to specific values |
| `CaseSensitive` | `bool` | Case-sensitive matching for FromAmong |
| `Secret` | `bool` | Redact value: help default shows `[REDACTED]`, errors omit the provided value |

### Restricted Values

```csharp
[CommandOption('l', "level", FromAmong = new[] { "debug", "info", "warn", "error" })]
public string Level { get; set; } = "info";
```

### Tokenizer Behavior

- Bare boolean flags never consume the next token: `--verbose` binds `true` and a following word stays positional (`--verbose off` sets `Verbose: True`, `Items: off`). Use `--verbose=off` for explicit values.
- `=`-form boolean literals accept `true/false/yes/no/on/off/1/0` (case-insensitive); anything else is an `InvalidValue` usage error (exit 2), e.g. `--verbose=maybe`.
- The first bare `--` ends option matching; every following token is positional, including `--verbose` and `--help`.
- Negative numbers (`-5`, `-1.5`) are positional without a separator.
- Combined shorts expand left to right: `-abc` binds each flag `true`; the last short takes the attached remainder (`-abdvalue` binds `Data: value`); an unknown char rejects the whole token (`Unknown option: -abx`, exit 2, `UnknownOption`). `-h`/`-V` inside a cluster win as help/version even mid-cluster.
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` is rejected as `InvalidValue` (exit 2). Unknown names report `Unknown option` (exit 2).
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); a valued repeat is a `DuplicateOption` usage error (exit 2).

## Arguments

Define positional arguments with `[CommandArgument]`:

```csharp
[CommandArgument(Name = "source", Position = 0, Description = "Source file", Required = true)]
public string SourceFile { get; set; } = "";

[CommandArgument(Name = "dest", Position = 1, Description = "Destination")]
public string? DestPath { get; set; }
```

### Argument Properties

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | Display name in help |
| `Position` | `int` | Positional index |
| `Description` | `string?` | Help text |
| `Required` | `bool` | Must be provided |
| `FromAmong` | `object[]` | Restrict to specific values |
| `CaseSensitive` | `bool` | Case-sensitive matching |
| `Secret` | `bool` | Redact value: errors omit the provided value |

## Shell Completion

Reserved gateway words intercepted after hierarchy build, before help/parsing (never dispatch to registered commands):

- `complete --position N "<commandline>"` — `N` is a 0-based character offset into the full command-line string (clamped to its length; defaults to end). Probes the hierarchy tolerantly, prints one candidate per line on stdout, exits `0`. Bare `complete` (no command line) lists root subcommands; other malformed input prints nothing, still `0`.
- `completions script <bash|zsh|pwsh|powershell|fish>` — prints a dotnet-style shim that re-invokes `complete --position N "<commandline>"` per TAB.

## Accessing Services

Mark a writable instance property with `[FromServices]` (unkeyed) or
`[FromKeyedServices(key)]` (keyed). The executor creates one
`IServiceScope` per command run, injects those properties from
`scope.ServiceProvider` after CLI binding, runs the command, then disposes
the scope after the lifetime callbacks. Scoped services are therefore
isolated to one command run; resolve additional services inside `Run` from
`applicationHost.Services` only when property injection does not fit.

```csharp
public class BuildCommand : Command
{
    [FromServices]
    public IMyService Service { get; set; } = null!;

    // Schematic — the upstream FromKeyedServicesAttribute targets parameters
    // only, so this line does NOT compile against the real framework
    // attribute (CS0592). Use the property-capable shim below instead.
    [FromKeyedServices("primary")]
    public IMyService Primary { get; set; } = null!;

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        // Service and Primary are already injected from the per-command scope.
    }
}
```

Compilable keyed path: define a same-named property-capable shim (or a
`using`-alias to one). The executor matches by attribute name and reads the
key from attribute metadata, so no new library dependency is needed. This is
exactly what `ServicePropertyInjectionTests` proves:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FromKeyedServicesAttribute(object key) : Attribute
{
    public object Key { get; } = key;
}
```

Rules:

- Disjoint sets: CLI-bound properties (`[CommandOption]` /
  `[CommandArgument]`) are never injected. A property marked with both a
  CLI attribute and a service attribute throws `InvalidOperationException`
  (surfaces as a fault, exit 1, never a usage error). Injection runs after
  binding, so CLI values are never overwritten.
- Keyed services resolve from the same per-command scope via
  `GetRequiredKeyedService(type, key)`.
- A missing service throws out of the executor and maps to a fault
  (exit 1), never a usage error (exit 2).
- Help, version, and validation paths return before the executor, so they
  construct zero scopes.
- No new attribute types: reuse the framework `[FromServices]` /
  `[FromKeyedServices]` markers. Note the upstream
  `FromKeyedServicesAttribute` targets parameters only, so compiler-applied
  property use is rejected (CS0592); the executor matches by attribute name
  and reads the key from attribute metadata.

## Command Lifecycle

Commands inherit the full `ApplicationDependency` lifecycle. See [Application Dependencies](application-dependencies.md) for details on `AddServices`, `AddConfigurations`, `AddMiddlewares`, `AddMappings`, `RunPreparation`, and `RunPreparationAsync`.

## Command Registration

Register a command by type with `AddCommand<TCommand>()` or by instance with `AddCommand(ICommand)`. Type registrations resolve a fresh instance on each `RunAsync` call so bound option values reset between runs; instance registrations reuse the same reference across runs. The command topology is rebuilt on every `RunAsync` from live registrations, so commands added between runs are visible on the next run.

## Exit Codes

| Outcome | Exit code |
|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`, `DuplicateOption`) | `2` |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` (custom host-code passthrough preserved) |
| Cancellation (`CancellationToken` / Ctrl+C) | `130` (128 + SIGINT) |

Return normally on success. Throw `CommandException` for errors:

```csharp
throw new CommandException("Operation failed", exitCode: 1);
```
