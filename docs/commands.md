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
| `FromAmong` | `object[]` | Restrict to specific values (enum-typed options auto-populate from the enum names when `FromAmong` is empty, unless a custom parser is registered for that enum type) |
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
- A bare valued option never consumes a flag-looking neighbor (reject-by-default): any dash-led non-numeric token — known or unknown, including `--help`/`--version` and the `--` separator — is left to bind or error on its own merits, while the valued option falls back to the trailing-bare missing sentinel (`null`, later satisfied by env fallback or `MissingRequired`, exit 2). `--config --verbose` binds `verbose=true` with `config` missing; `--config --nope` reports `Unknown option: --nope` (exit 2); `--config --version` fires version. `=`-form (`--config=f.json`), compact (`-cf.json`), and numeric neighbors (`--seed -5`) still bind as values; use `--` to pass a dash-led value positionally.
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
| `FromAmong` | `object[]` | Restrict to specific values (enum-typed arguments auto-populate from the enum names when `FromAmong` is empty, unless a custom parser is registered for that enum type — same rule as options) |
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
- `completions install [--shell <bash|zsh|pwsh|fish>] [--dry-run]` — writes the shim into the shell startup file inside a guarded `# >>> <exe> completion >>>` / `# <<< <exe> completion <<<` block (replace-in-place, append when absent; missing rc is created). Without `--shell`, the basename of `$SHELL` is used (`powershell` maps to `pwsh`). Targets:
| Shell | Target |
|---|---|
| bash | `~/.bashrc` |
| zsh | `~/.zshrc` |
| pwsh | Windows: `~/Documents/PowerShell/Microsoft.PowerShell_profile.ps1`; elsewhere: `$XDG_CONFIG_HOME/powershell/Microsoft.PowerShell_profile.ps1`, else `~/.config/powershell/Microsoft.PowerShell_profile.ps1` |
| fish | `$XDG_CONFIG_HOME/fish/completions/<exe>.fish`, else `~/.config/fish/completions/<exe>.fish` |
`XDG_CONFIG_HOME` is honored only when absolute; a relative, empty, or unreadable value falls back to `~/.config` (applies to fish and non-Windows pwsh). Fish idempotence is byte-exact over UTF-8-no-BOM bytes, so a stale encoding counts as drift and reinstalls. Byte-identical re-runs print `already installed: <path>` without rewriting; otherwise prints `installed: <path>`, exit `0`. `--dry-run` prints `would-write: <path>` plus the content and changes nothing. Mutating install/uninstall paths hold a per-target sibling `<target>.lock` (same-target serializes ≤10s then fails loudly, different targets never block, dry-run never locks); a lock timeout reports on stderr, exit `1`.
- `completions uninstall [--shell <...>]` — removes only the managed block; missing file or no block prints `not installed: <path>`, exit `0` (rc files are never deleted; only a fully-managed fish file is deleted). A fish file without the managed block is left untouched and refused on stderr, exit `1`.
- `completions install` / `uninstall` with an unknown shell (including undetectable `$SHELL`) report on stderr, exit `2`. `completions script <unknown>` is handled (returns `true`): it reports `Unknown shell '<shell>'. Expected bash, zsh, pwsh, or fish.` on stderr via the shared `CompletionInstaller.TryCanonicalizeShell` canonicalizer (`CompletionGateway.cs:196-202`), exit `2` — never the parse-path `No command found`. IO failures report on stderr, exit `1` (lock/permission failures name the lock path; permission failures say "Access denied"). Executable names are validated (ASCII letters, digits, `.`, `_`, `-`, max 64 chars, starting with a letter or `_`; empty falls back to `myapp`) — anything else reports the allowed set on stderr, exit `2`.

Exit matrix (`CompletionGateway.cs:24-57,106-204`):

| Input | Exit | Notes |
|---|---|---|
| `complete [...]` (any probe, incl. bare/malformed) | `0` | Tolerant probe: malformed input prints nothing, still `0` (`:100-103`) |
| `completions script <known shell>` | `0` | Extra tokens (e.g. `--help`) ignored (`:39-44`; test `CompletionsScript_IgnoresTrailingHelp`) |
| `completions install` / `uninstall` success | `0` | Includes `already installed` / `not installed` no-ops |
| `completions install` / `uninstall` unknown option, unknown shell, or invalid exe name | `2` | stderr (`:127-128,:169-170,:218-230`) |
| `completions script <unknown shell>` | `2` | Handled (`true`): `Unknown shell '<shell>'. Expected bash, zsh, pwsh, or fish.` on stderr via shared `TryCanonicalizeShell` (`:196-202`); never the parse path |
| `completions install` / `uninstall` IO failure (incl. lock timeout) | `1` | stderr (`:141-150,:183-192`); fish foreign-file refusal surfaces here |
| Bare `completions`, `completions script` (no shell), `completions <unknown>` | falls through to parse | Returns `false`; parse reports `No command found`, exit `2` (`:36-37,:41-42,:56`) |

## Accessing Services

Mark a writable instance property with `[FromServices]` (unkeyed) or
`[FromKeyedServices(key)]` (keyed). Owner: `ServiceInjectionGate`
(`src/ApplicationBuilderHelpers/CommandLineParser/ServiceInjectionGate.cs:40-120`,
single `Inject` entry at `:101`). The thin `CommandExecutor`
(`src/ApplicationBuilderHelpers/CommandLineParser/CommandExecutor.cs:19-33`)
creates one `IServiceScope` per command run, then the gate injects those
properties from `scope.ServiceProvider` after CLI binding; the command runs,
then the scope is disposed after the lifetime callbacks. Scoped services are
therefore isolated to one command run; resolve additional services inside
`Run` from `applicationHost.Services` only when property injection does not fit.

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
`using`-alias to one). The gate matches by attribute simple name
(`ServiceInjectionGate.cs:24-30,65-66` — reuse-only seam, no new library
dependency) and reads the key from attribute metadata (`:140-168`), so no
new library dependency is needed. This is
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
  from the gate's single fail-fast point (`ServiceInjectionGate.cs:127-133`,
  exact historical message preserved; checked both in the cached plan at
  `:72-76` and at inject time at `:109-112`) — surfaces as a fault,
  exit 1, never a usage error. Injection runs after
  binding, so CLI values are never overwritten.
  Fault-path re-verify: an injection throw propagates out of
  `CommandExecutor.ExecuteCommand` (`CommandExecutor.cs:86`) before the
  orchestrator runs, so no `Exiting` callback fires and only the `Exited`
  `finally` at `:94-99` runs — the same fault-path shape as a faulted
  command/host win.
- Keyed services resolve from the same per-command scope via
  `GetRequiredKeyedService(type, key)`.
- A missing service throws out of the executor and maps to a fault
  (exit 1), never a usage error (exit 2).
- Help, version, and validation paths return before the executor, so they
  construct zero scopes.
- No new attribute types: reuse the framework `[FromServices]` /
  `[FromKeyedServices]` markers. Note the upstream
  `FromKeyedServicesAttribute` targets parameters only, so compiler-applied
  property use is rejected (CS0592); the gate matches by attribute name
  and reads the key from attribute metadata.

## Command Lifecycle

Commands inherit the full `ApplicationDependency` lifecycle. See [Application Dependencies](application-dependencies.md) for details on `AddServices`, `AddConfigurations`, `AddMiddlewares`, `AddMappings`, `RunPreparation`, and `RunPreparationAsync`.

### Lifetime Callbacks

Register via `LifetimeService` (`applicationHost.Services.GetRequiredService<LifetimeService>()`): `ApplicationExitingCallback` runs when the command/host is stopping, `ApplicationExitedCallback` runs after shutdown. Owner of the call sites: `CommandRunOrchestrator` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOrchestrator.cs:23-99`) owns the joint command/host run and the exactly-once `Exiting` fan-out — command-wins canceled (`:52`), host-wins canceled (`:78`), host-won-success-but-command-canceled OCE-only (`:86-89`) — plus the executor's trailing success `Exiting` (`CommandExecutor.cs:90-92`). The `LifetimeGlobalService` `Interlocked.Exchange` guards (`src/ApplicationBuilderHelpers/Services/LifetimeGlobalService.cs:17-22,58-86`) are retained fail-safe: the first caller wins, late callers no-op. On success and cancellation (`130`) paths — whether the command or the host wins the shutdown race — each runs exactly once per `RunAsync`. On fault paths the exception rethrows before any `Exiting` invocation (command-wins faulted at `CommandRunOrchestrator.cs:41-47`, host-wins faulted at `:69-74`, injection-throw fault path at `CommandExecutor.cs:86`), so only `Exited` runs via the null-guarded `finally` (`CommandExecutor.cs:94-99`).

Fail-safe proof: `AbsolutePathAndLifetimeTests.LifetimeGlobalService_ExitingDoubleInvoke_RunsOnce` and `..._ExitedDoubleInvoke_RunsOnce` (`src/ApplicationBuilderHelpers.Test.Cli.UnitTest/AbsolutePathAndLifetimeTests.cs:222-260`) invoke each callback set twice and assert each action/task ran exactly once.

### Lifecycle stages (landed file map)

Thin sequencer `CommandExecutor` (`CommandLineParser/CommandExecutor.cs:19-33`) over collaborators — mechanical split, no behavior change:

| Stage | Owner | Landed path |
|---|---|---|
| Shutdown scope (linked CTS joining outer token + Ctrl+C; host `ApplicationStopping` stays host-owned downstream) + Ctrl+C subscribe/dispose | `CommandShutdownScope` | `src/ApplicationBuilderHelpers/CommandLineParser/CommandShutdownScope.cs:6-14,23-46` |
| Console cancel signal (injectable; production forwarder) | `IConsoleCancelSignal` / `ConsoleCancelSignal` | `src/ApplicationBuilderHelpers/CommandLineParser/IConsoleCancelSignal.cs:14-26`, `src/ApplicationBuilderHelpers/CommandLineParser/ConsoleCancelSignal.cs:12-38` |
| Console adapter only (Out/Error routing + `CancelKeyPress` forwarder) | `ConsoleOutput` | `src/ApplicationBuilderHelpers/CommandLineParser/ConsoleOutput.cs:6-35` |
| Per-command service injection (single `Inject` entry) | `ServiceInjectionGate` | `src/ApplicationBuilderHelpers/CommandLineParser/ServiceInjectionGate.cs:40-41,101-120` |
| Joint command/host run + exactly-once `Exiting` fan-out | `CommandRunOrchestrator` → `CommandRunOutcome` | `src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOrchestrator.cs:23-30`, `src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOutcome.cs:8-41` |
| Single cancel-wins classification point | `CommandExitMapper` | `src/ApplicationBuilderHelpers/CommandLineParser/CommandExitMapper.cs:6-40` |
| Exactly-once guards (fail-safe) | `LifetimeGlobalService` | `src/ApplicationBuilderHelpers/Services/LifetimeGlobalService.cs:17-22,58-86` |

## Command Registration

Register a command by type with `AddCommand<TCommand>()` or by instance with `AddCommand(ICommand)`. Type registrations resolve a fresh instance on each `RunAsync` call so bound option values reset between runs; instance registrations reuse the same reference across runs. The command topology is rebuilt on every `RunAsync` from live registrations, so commands added between runs are visible on the next run.

## Exit Codes

Single classification point: `CommandExitMapper` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandExitMapper.cs:18-40`, cancel-wins `IsExternalAbort(shutdown, outer, ctrlC)` at `:25-28`) — cancellation observed via the outer token or Ctrl+C before host completion maps to `130`; internal-only cooperative cancellation stays success (`0`). The executor catch filter (`CommandExecutor.cs:101-104`) and the orchestrator `ThrowIfExternalAbort` (`CommandShutdownScope.cs:74-75` → `CommandExitMapper.cs:34-40`) both funnel through it.

| Outcome | Exit code |
|---|---|
| `Run` returns normally (also `--help` / `--version`); internal-only cooperative `OperationCanceledException` | `0` (`CommandLineParser.cs:131-135`) |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`, `DuplicateOption`) | `2` |
| Unexpected fault (`Fault`, `NoImplementation`) or `Run` throwing `CommandException` | `1`, or `ex.ExitCode` passthrough (`CommandException.cs:13,43-46`; non-zero host-winner throws `CommandException` at `CommandRunOrchestrator.cs:91-94`; surfaced at `CommandLineParser.cs:121-125`) |
| External cancellation (outer `CancellationToken` / Ctrl+C, incl. pre-cancelled token) | `130` — Unix 128 + SIGINT convention (`CommandExecutor.cs:39`; `ExternalCancellationException` at `:45-51` always maps to it; surfaced at `CommandLineParser.cs:116-119,126-129`). Windows note: Windows has no SIGINT exit-code convention — a Ctrl+C kill tears the process down at OS level with its own status — so `130` is the library-level cancellation mapping on all platforms (`CommandExitMapper.cs:13-17`, code remark only). |

Return normally on success. Throw `CommandException` for errors:

```csharp
throw new CommandException("Operation failed", exitCode: 1);
```
