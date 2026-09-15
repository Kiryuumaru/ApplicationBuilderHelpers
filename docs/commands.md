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
| `Secret` | `bool` | Redact value: help default shows `[REDACTED]`, errors omit the provided value (including `--no-<name>=value` rejections for secret valued options) |

### Environment Variable Fallback

When `EnvironmentVariable` is set and no CLI token is supplied, the env value fills the option — except an empty or whitespace-only env value is treated as unset. Precedence is CLI-wins: an explicit CLI token always replaces the env value, so `--opt ""` downgrades a set env value to `""` for string targets.

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
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` never accepts a value. A known name (flag, valued, or collection — including secret valued options resolved through the command's full option scope) is rejected as `InvalidValue` (exit 2) with secret-aware text that omits the value for secrets; an unknown name reports `Unknown option: --no-<name>` (exit 2) with a name-only suggestion, never echoing the value; an empty base (`--no-=value`) fails closed as `InvalidValue` (exit 2) with redaction on.
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); a valued repeat is a `DuplicateOption` usage error (exit 2).
- Unknown options report name-only (fail-closed logging): `--pasword=hunter2` reports `Unknown option: --pasword` (exit 2) with a name-only suggestion, never echoing the value. Combined-short clusters contain no `=`, so the whole-token report (`Unknown option: -abx`) is already name-only.

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

A positional argument is present when its token is supplied — even as `""` — which satisfies `Required`, while an omitted required argument fails with `MissingRequired` (exit 2); for string-typed targets the token binds verbatim as `""` (a whitespace-only token is likewise preserved, not trimmed). Non-string `""` follows per-type parser semantics instead: unparseable types report `InvalidValue` (exit 2). Named `bool` options reject `""` in `=`-form (`InvalidValue`, exit 2, same rule as the tokenizer section above) and never consume a following `""` in space-form — it stays positional; the `BoolTypeParser` empty-binds-`true` path is reachable only for positional `bool` arguments, which pass through with no literal gate. The same preserve rule applies to named options, except an empty or whitespace-only environment-variable fallback is treated as unset, and any strict empty-rejecting mode is a separate follow-up.

**Breaking change:** code that relied on `""` arriving as `null` (e.g. `== null` sentinels) must migrate to `string.IsNullOrEmpty` — an explicitly supplied `""` now binds as `""`, never `null`.

## Shell Completion

Owner: `CompletionGateway` (`src/ApplicationBuilderHelpers/CommandLineParser/CompletionGateway.cs:17-19`, ctor `ICommandBuilder` + `ConsoleOutput`) delegating to `CompletionEngine` (probe), `CompletionScriptWriter` (script), `CompletionInstaller` (install/uninstall). Wired in `CommandLineParser` after hierarchy build, before help/parsing.

Precedence: completion > help > parse > version — the gateway runs at `CommandLineParser.cs:68-70` after hierarchy build (`:65-66`), before bare `--help` (`:73`), before `ParseCommandLine` (`:80`), before the post-parse version check (`:83-87`).

> Shadowing warning: gateway words never dispatch to registered commands. A user-registered `complete` or `completions install` command never runs — the gateway handles first (`CompletionGateway.cs:10-15`).

Reserved gateway words intercepted after hierarchy build, before help/parsing (never dispatch to registered commands):

- `complete --position N "<commandline>"` — `N` is a 0-based character offset into the full command-line string (clamped to its length; defaults to end). Probes the hierarchy tolerantly, prints one candidate per line on stdout, exits `0`. Bare `complete` (no command line) lists root subcommands; other malformed input prints nothing, still `0`.
- `completions script <bash|zsh|pwsh|powershell|fish>` — prints a dotnet-style shim that re-invokes `complete --position N "<commandline>"` per TAB.
- `completions install [--shell <bash|zsh|pwsh|fish>] [--dry-run]` — writes the shim into the shell startup file inside a guarded `# >>> <exe> completion >>>` / `# <<< <exe> completion <<<` block (replace-in-place, append when absent; missing rc is created). Without `--shell`, the basename of `$SHELL` is used (`powershell` maps to `pwsh`). Targets: bash `~/.bashrc`, zsh `~/.zshrc`, pwsh per-OS profile (`~/Documents/PowerShell/Microsoft.PowerShell_profile.ps1` on Windows, `~/.config/powershell/...` elsewhere), fish `~/.config/fish/completions/<exe>.fish` (honors `XDG_CONFIG_HOME`). Byte-identical re-runs print `already installed: <path>` without rewriting; otherwise prints `installed: <path>`, exit `0`. `--dry-run` prints `would-write: <path>` plus the content and changes nothing.
- `completions uninstall [--shell <...>]` — removes only the managed block; missing file or no block prints `not installed: <path>`, exit `0` (rc files are never deleted). A fish file without the managed block is left untouched and refused on stderr, exit `1`.
- `completions install` / `uninstall` with an unknown shell (including undetectable `$SHELL`) report on stderr, exit `2`. `completions script <unknown>` instead falls through to the parse path (`No command found`, exit `2`) — never the installer `Unknown shell` path. IO failures report on stderr, exit `1`.

Exit matrix (`CompletionGateway.cs:24-57,106-193`):

| Input | Exit | Notes |
|---|---|---|
| `complete [...]` (any probe, incl. bare/malformed) | `0` | Tolerant probe: malformed input prints nothing, still `0` (`:100-103`) |
| `completions script <known shell>` | `0` | Extra tokens (e.g. `--help`) ignored (`:39-44`; test `CompletionsScript_IgnoresTrailingHelp`) |
| `completions install` / `uninstall` success | `0` | Includes `already installed` / `not installed` no-ops |
| `completions install` / `uninstall` unknown option or unknown shell | `2` | stderr (`:127-128,:169-170,:218-230`) |
| `completions install` / `uninstall` IO failure | `1` | stderr (`:141-150,:183-192`); fish foreign-file refusal surfaces here |
| Bare `completions`, `completions script` (no shell), `completions script <unknown>`, `completions <unknown>` | falls through to parse | Returns `false`; parse reports `No command found`, exit `2` (`:36-37,:41-42,:56`; tests `:126-167`) |

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

### Lifetime Callbacks

Register via `LifetimeService` (`applicationHost.Services.GetRequiredService<LifetimeService>()`): `ApplicationExitingCallback` runs when the command/host is stopping, `ApplicationExitedCallback` runs after shutdown. On success and cancellation (`130`) paths — whether the command or the host wins the shutdown race — each runs exactly once per `RunAsync`. On fault paths the exception rethrows before the trailing `Exiting` invocation, so only `Exited` runs.

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
