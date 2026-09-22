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

When `EnvironmentVariable` is set and no CLI token is supplied, the env value fills the option — except an empty or whitespace-only env value is treated as unset. Precedence is CLI-wins: an explicit CLI token always replaces the env value, so `--opt ""` downgrades a set env value to `""` for string targets. A typed bare occurrence (`--config` with no value) never qualifies as "no CLI token" — env rescues only omitted (never-typed) options, never an explicit bare.

### Required Options in Help

A required option (`Required = true`) renders the verbatim lowercase `(required)` marker on the line immediately after its description. Description lines follow a fixed ordinal (`src/ApplicationBuilderHelpers/CommandLineParser/HelpContentProvider.cs:288-322`): Description (`:292-295`) → `(required)` (`:297-300`) → `Possible values: ...` (`:302-306`) → `Environment variable: ...` (`:308-311`). There is no `Default:` line when `IsRequired` (`:313-318`).

- `Default:` suppression: a required option never shows a `Default:` line, even when the CLR type carries an implicit default. The motivating case is a required `int` (e.g. `[CommandOption("count", Description = "Item count.", Required = true)]`), which omits the phantom `Default: 0` (`RequiredOptionHelpTests.cs:47-58`). A required `string` likewise shows the marker with no `Default:` line (`:61-72`), while an optional `int` with an explicit initializer keeps its `Default:` line (e.g. `Default: 3`, `:75-86`).
- Env interplay: a required option with an `EnvironmentVariable` fallback still shows the `Environment variable: ...` line after `(required)` (and after `Possible values:` when `FromAmong` is set): Description → `(required)` → `Possible values:` → `Environment variable:` (`RequiredOptionHelpTests.cs:89-102`).
- Secret interplay: suppression beats redaction — a required secret option shows `(required)` but never a `Default: [REDACTED]` line, because the `Default:` arm is skipped before `SecretRedaction.GetDefaultDisplay` is reached (`HelpContentProvider.cs:313-317`; mask defined at `src/ApplicationBuilderHelpers/CommandLineParser/SecretRedaction.cs:40-46`).
- Unchanged: option signatures, usage `[OPTIONS]`, and two-column layout are untouched — only the description lines change.

### Restricted Values

```csharp
[CommandOption('l', "level", FromAmong = new[] { "debug", "info", "warn", "error" })]
public string Level { get; set; } = "info";
```

### Tokenizer Behavior

- Bare boolean flags never consume the next token: `--verbose` binds `true` and a following word stays positional (`--verbose off` sets `Verbose: True`, `Items: off`). Use `--verbose=off` for explicit values.
- A bare valued option never consumes a flag-looking neighbor (reject-by-default): any dash-led non-numeric token — known or unknown, including `--help`/`--version` and the `--` separator — is left to bind or error on its own merits, while the valued option falls back to the trailing-bare missing sentinel (`null`, enforced in `ParameterValidator.cs:20-71`). A bare occurrence with no merged value is a missing value on its own merits — required or optional alike — and always fails `MissingRequired` (exit 2) even with env set: `Missing value for option: <display-name>` (optional) or `Missing required option: <display-name>` (required). Typing the option claims ownership, so env fallback never rescues a typed bare; it applies only to omitted (never-typed) options (`EnvVarFallback.cs:21-39`; the required rescue at `ParameterValidator.cs:29-31` is gated on no bare mark, and the optional pass at `:52-71` has no rescue call). A single trailing-bare fails even with env set; a satisfied required valued scalar repeated bare (`--name John ... --name` at end-of-line) fails `MissingRequired` (exit 2) regardless of env fallback. Precedence: an unknown neighbor errors on its own merits first — `--config --nope` reports `Unknown option: --nope` (exit 2) because the parser throws before validation runs; `--help`/`--version` neighbors keep their carve-out — `--config --help` shows help and `--config --version` fires version, since the optional-bare pass is skipped when `ShowHelp`/`ShowVersion` is set. Conversion beats help-with-values only (#483): binding errors collect at Step 7 through the same conversion pipeline (`CommandLineParser.cs:99-115`; dry-run `ValueBinder.cs:29-65`), so an `InvalidValue` (exit 2) surfaces before help-with-values; `MissingRequired` already won in the same step. Version path untouched — the post-parse version check (`CommandLineParser.cs:83-87`) runs before validation, so invalid+version still exits `0`. Bare carve-out preserved: the collect skips bare-ledger keys when `ShowHelp` is set (`ValueBinder.cs:46`). `--config --verbose` fails `Missing value for option: -c, --config` (exit 2) whether or not `TEST_CONFIG` is set — remove the flag to use the env value. `=`-form (`--config=f.json`), compact (`-cf.json`), and numeric neighbors (`--seed -5`) still bind as values; use `--` to pass a dash-led value positionally.
- `=`-form boolean literals accept `true/false/yes/no/on/off/1/0` (case-insensitive); anything else is an `InvalidValue` usage error (exit 2), e.g. `--verbose=maybe`.
- Help-reserved `=`/negated forms never bind help — they are `InvalidValue` usage errors (exit 2): `--help=<anything>` (including empty `--help=`), `-h=<anything>` (including empty `-h=`) unless a real short-`h` owner exists (e.g. `serve --host`, where `-h=<value>` parses as that option), bare `--no-help`, and `--no-help=<anything>` (`ArgumentParser.cs:433-450` gate; `:460-481` error). Bare `--help`/`-h` still show help (exit 0); `--help false` shows help with `false` left positional. Post-`--` tokens stay positional and never hit this gate. On an abstract prefix (e.g. `config --help=x`) the reserved form reports `InvalidValue` (exit 2) instead of `RequiresSubcommand`.
- The first bare `--` ends option matching; every following token is positional, including `--verbose` and `--help`.
- Negative numbers (`-5`, `-1.5`) are positional without a separator — and a bare numeric token wins over a digit short: `-1`, `-10`, `-1.5` never bind a `ShortTerm '0'`–`'9'` option even when one is declared (guard at `ArgumentParser.cs:130-140`; numeric test `IsNumericValue` at `:393-406`). The digit short stays reachable only via in-token forms (`-1=value` per `SubCommandOptionInfo.cs:365,391-397`, compact `-1x` for valued options per `:369,412-415`) or after `--`.
- Combined shorts expand left to right: `-abc` binds each flag `true`; the last short takes the attached remainder (`-abdvalue` binds `Data: value`); an unknown char rejects the whole token (`Unknown option: -abx`, exit 2, `UnknownOption`). `-h`/`-V` inside a cluster win as help/version even mid-cluster.
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` never accepts a value. A known name (flag, valued, or collection — including secret valued options resolved through the command's full option scope) is rejected as `InvalidValue` (exit 2) with secret-aware text that omits the value for secrets; an unknown name reports `Unknown option: --no-<name>` (exit 2) with a name-only suggestion, never echoing the value; an empty base (`--no-=value`) fails closed as `InvalidValue` (exit 2) with redaction on.
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); valued repeats are last-wins (`--text=a --text=b` binds `b`), except a trailing bare repeat of a satisfied required valued scalar fails `MissingRequired` (exit 2) regardless of env fallback, while a trailing bare repeat of a satisfied optional is ignored and the prior value stands (`ParameterValidator.cs:64-65` merged-values gate). Unsatisfied bare (`--config` with no value anywhere) is not a repeat — it fails per the tokenizer bullet above even with env set. Bare-then-valued heals; collections accumulate; bare boolean flags stay idempotent.
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
(`src/ApplicationBuilderHelpers/CommandLineParser/ServiceInjectionGate.cs:40-126`,
single `Inject` entry at `:111`). The thin `CommandExecutor`
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
(`ServiceInjectionGate.cs:24-30,75-77` — reuse-only seam, no new library
dependency) and reads the key from attribute metadata (`:146-174`), so no
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

- Canonical bound identity: a property is CLI-bound iff it carries
  `[CommandOption]` or `[CommandArgument]`. The predicate lives once as
  `CommandReflectionCache.IsCliBound` (`:212-216`), next to `Walk` (`:229-248`),
  and both the reflection cache (`Build` at `:131-162`) and the injection
  plan (`ServiceInjectionGate.cs:51-109`) call it. The plan additionally
  hoists a bound-name set derived once from that same predicate over the
  walk (`:53-70`) — so cross-entry hide conflicts (base CLI + derived
  service under one name) throw too — never re-derived per `Inject` call
  from the per-run `AllOptions`/`AllArguments` view (that second source
  is deleted).
- Disjoint sets: CLI-bound properties (`[CommandOption]` /
  `[CommandArgument]`) are never injected. A property marked with both a
  CLI attribute and a service attribute throws `InvalidOperationException`
  from the gate's single fail-fast point (`ServiceInjectionGate.cs:133-139`,
  exact historical message preserved) — surfaces as a fault,
  exit 1, never a usage error. Injection runs after
  binding, so CLI values are never overwritten.
  Fault-path re-verify: an injection throw propagates out of
  `CommandExecutor.ExecuteCommand` (`CommandExecutor.cs:86`) before the
  orchestrator runs, so no `Exiting` callback fires and only the `Exited`
  `finally` at `:94-99` runs — the same fault-path shape as a faulted
  command/host win.
- Always-error on hiding: member hiding (`new`) never excuses a conflict.
  The walk keeps hidden members as base-first duplicates
  (`CommandReflectionCache.cs:229-248`, characterization
  `Options_HiddenMember_CharacterizesCurrentWalk`), so any dual-marked
  `PropertyInfo` anywhere in the walk chain throws — checking the hidden
  derived entry alone is not enough.
- Injection plans are cached: the per-`Type` target list (property plus
  optional keyed-service key) is built once under the shared
  double-checked lock (`TypePlanCache.cs`; gate use at
  `ServiceInjectionGate.cs:44-48`) and reused across runs; the CLI-bound
  set is hoisted into the cached plan at `:53-70`. Reflection descriptors
  are cached separately per builder through the same shared core
  (`CommandReflectionCache.cs:107-125`, miss-counted by `BuildCount` at
  `:109-112`; shared core at `TypePlanCache.cs`). Marker matching stays
  narrow: by attribute simple name (`ServiceInjectionGate.cs:24-30,75-77`
  — reuse-only seam, no new library dependency) with the key read from
  attribute metadata (`:146-174`).

```csharp
// Dual-marked example — always throws InvalidOperationException (fault, exit 1):
public class BadCommand : Command
{
    [CommandOption("name")]
    [FromServices] // configuration error: CLI-bound AND service-marked
    public IMyService Service { get; set; } = null!;
}
```
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
| Per-command service injection (single `Inject` entry) | `ServiceInjectionGate` | `src/ApplicationBuilderHelpers/CommandLineParser/ServiceInjectionGate.cs:40-44,111-126` |
| Joint command/host run + exactly-once `Exiting` fan-out | `CommandRunOrchestrator` → `CommandRunOutcome` | `src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOrchestrator.cs:23-30`, `src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOutcome.cs:8-41` |
| Single cancel-wins classification point | `CommandExitMapper` | `src/ApplicationBuilderHelpers/CommandLineParser/CommandExitMapper.cs:6-40` |
| Exactly-once guards (fail-safe) | `LifetimeGlobalService` | `src/ApplicationBuilderHelpers/Services/LifetimeGlobalService.cs:17-22,58-86` |

## Command Registration

Register a command by type with `AddCommand<TCommand>()` or by instance with `AddCommand(ICommand)`. Type registrations resolve a fresh instance on each `RunAsync` call so bound option values reset between runs; instance registrations reuse the same reference across runs. The command topology is rebuilt on every `RunAsync` from live registrations, so commands added between runs are visible on the next run.

## Exit Codes

Single classification point: `CommandExitMapper` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandExitMapper.cs:18-40`, cancel-wins `IsExternalAbort(shutdown, outer, ctrlC)` at `:25-28`) — cancellation observed via the outer token or Ctrl+C before host completion maps to `130`; internal-only cooperative cancellation stays success (`0`). The executor catch filter (`CommandExecutor.cs:101-104`) and the orchestrator `ThrowIfExternalAbort` (`CommandShutdownScope.cs:74-75` → `CommandExitMapper.cs:34-40`) both funnel through it.

| Outcome | Exit code |
|---|---|
| `Run` returns normally (also `--help` / `--version`); internal-only cooperative `OperationCanceledException` | `0` (`CommandLineParser.cs:131-135`; conversion failure beats help-with-values, invalid+version still `0` via the pre-validation version guard at `:83-87`) |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` is reserved and never thrown — valued repeats resolve last-wins) | `2` — `MissingRequired` also covers an explicit bare valued option, which fails even with env set (`Missing value for option: <display-name>` for optional, `Missing required option: <display-name>` for required; `ParameterValidator.cs:20-71`; env rescues only omitted options). Missing and invalid failures aggregate: every missing error reports first, then every invalid-value error, joined with newlines in one exit-`2` failure (missing-only keeps kind `MissingRequired`, any invalid line makes the kind `InvalidValue`); conversion failure beats help-with-values via the Step 7a probe while missing errors keep winning over help |
| Unexpected fault (`Fault`, `NoImplementation`) or `Run` throwing `CommandException` | `1`, or `ex.ExitCode` passthrough (`CommandException.cs:13,43-46`; non-zero host-winner throws `CommandException` at `CommandRunOrchestrator.cs:91-94`; surfaced at `CommandLineParser.cs:121-125`) |
| External cancellation (outer `CancellationToken` / Ctrl+C, incl. pre-cancelled token) | `130` — Unix 128 + SIGINT convention (`CommandExecutor.cs:39`; `ExternalCancellationException` at `:45-51` always maps to it; surfaced at `CommandLineParser.cs:116-119,126-129`). Windows note: Windows has no SIGINT exit-code convention — a Ctrl+C kill tears the process down at OS level with its own status — so `130` is the library-level cancellation mapping on all platforms (`CommandExitMapper.cs:13-17`, code remark only). |

Return normally on success. Throw `CommandException` for errors:

Help/footer contract (see [Advanced Topics](advanced.md#error-footers) and [Advanced Topics](advanced.md#help-system)): every help screen (global and per-command) lists `-V, --version` under `GLOBAL OPTIONS:` (`src/ApplicationBuilderHelpers/CommandLineParser/HelpContentProvider.cs:105-110,220-225`); usage-error footers hint at both `--help` and `--version` (`src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:15-36`), while `Fault`/`NoImplementation` keep the single-sentence `--help`-only footer (`CommandErrorFooter.cs:37-38`). Precedence is unchanged: completion > help > parse > version.

```csharp
throw new CommandException("Operation failed", exitCode: 1);
```
