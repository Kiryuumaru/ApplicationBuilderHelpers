# Advanced Topics

## Sub-Commands

Build hierarchical command structures with space-separated names:

```csharp
[Command("deploy", description: "Deployment operations")]
public class DeployCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine("Use a sub-command: deploy prod, deploy staging");
        return ValueTask.CompletedTask;
    }
}

[Command("deploy prod", description: "Deploy to production")]
public class DeployProductionCommand : Command { /* ... */ }

[Command("deploy prod rollback", description: "Rollback production")]
public class DeployProductionRollbackCommand : Command { /* ... */ }
```

```bash
myapp deploy                    # Shows sub-command help
myapp deploy prod               # Runs production deploy
myapp deploy prod rollback      # Runs rollback with 3-part name
```

Arbitrary nesting depth is supported.

## Bare Root and Help-First

When only leaf subcommands are registered (no root implementation), the root is abstract (`SubCommandInfo.IsRoot` at `src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:130`; display name `"<root>"` at `:32`):

- Bare run (`[]`) → exit `2`: `'<root>' requires a subcommand. Available subcommands: ...` (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:93-105`). The structured `CommandName` stays empty for root (`:95`), so the footer is the two-sentence global usage footer (`Run '<exe> --help' to see available commands and options. Run '<exe> --version' to show version information.`, `src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:20-23`). No `Did you mean` pointer on a bare run (nothing to match).
- Help-first (`IsRoot || argIndex > 0` + help token pre-`--`, `ArgumentParser.cs:66-70`) → exit `0` without erroring. Only the root maps to the **global** help model (`COMMANDS:` section): `HelpFormatter` branches on `IsRoot` alone (`src/ApplicationBuilderHelpers/CommandLineParser/HelpFormatter.cs:42-44`), so a named abstract parent (e.g. `config hub`) keeps its parent-scoped view (`BuildCommandModel`), pinned by `Spaced_Abstract_Base_Matches_Normalized_Path` asserting `"Spaced hub."`:

| Input | Exit | Result |
|---|---|---|
| `[]` | `2` | `'<root>' requires a subcommand` + global footer |
| `["--help", "greet"]` | `0` | Root/global help (`USAGE:` + `<COMMAND>` + `COMMANDS:` + `GLOBAL OPTIONS:`), never `greet` help |
| `["--help", "--bogus"]` | `0` | Root/global help (unknown trailing flag ignored on the help-first path) |
| `["bogus", "--help"]` | `2` | `No command found for 'bogus'` (`UnknownCommand` — zero-match wins before help) |
| `["--", "--help"]` | `2` | `'<root>' requires a subcommand` (the `--` sentinel blocks the help carve-out) |
| `["config", "hub", "--help"]` | `0` | Parent-scoped help (`"Spaced hub."`) — globalization is root-only |
| `["--verbose=banana"]` | `2` | `Invalid Boolean value 'banana' for option '--verbose'. Expected ...` (`InvalidValue` — #542 beats `RequiresSubcommand` when the base is a root-visible flag) |
| `["--verbose=true"]` | `2` | `'<root>' requires a subcommand` (valid literal falls through to `RequiresSubcommand`) |
| `["--", "--verbose=banana"]` | `2` | `'<root>' requires a subcommand` (the `--` sentinel blocks the #542 gate) |
| `["--data"]` | `2` | `Missing value for option: -d, --data` (`MissingRequired` — #549 beats `RequiresSubcommand` when a root-visible valued option is typed bare, even with env set) |
| `["--data=x"]` | `2` | `'<root>' requires a subcommand` (valid valued form falls through — no option error exists) |
| `["-vd"]` | `2` | `Missing value for option: -d, --data` (cluster valued-short bare tail, same #549 gate) |

Pinned by `AbstractRootRequiresSubcommandTests.cs` (13 tests) + `AbstractRootFlagLiteralTests.cs` (17 tests: invalid/empty/short-form literals, valid-literal fall-through, secret redaction, valued/bare/cluster fall-through, sentinel silence, `--no-` unchanged, leaf-only unknown, abstract-prefix command name) + `AbstractRootBareOptionTests.cs` (#549: bare-long/short/cluster-tail/env-bare beat `RequiresSubcommand`; valid-valued/surplus/sentinel/help/version fall through).

## Multiple Host Types

### Console Apps (Default)

```csharp
public class MyCommand : Command  // Uses HostApplicationBuilder
```

### Custom Host Types

```csharp
public class WebCommand : Command<WebApplicationBuilder>
{
    protected override ValueTask<WebApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = WebApplication.CreateBuilder();
        return new ValueTask<WebApplicationBuilder>(builder);
    }

    protected override async ValueTask Run(ApplicationHost<WebApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        var app = applicationHost.Builder.Build();
        app.MapGet("/", () => "Hello World");
        await app.RunAsync();
    }
}
```

Any type implementing `IHostApplicationBuilder` is supported.

## Exit Codes

| Outcome | Exit code |
|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` (conversion failure beats help-with-values; invalid+version still `0` via the pre-validation version guard at `CommandLineParser.cs:83-87`) |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` is reserved and never thrown — valued repeats resolve last-wins) | `2` |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` (custom host-code passthrough preserved) |
| Cancellation (`CancellationToken` / Ctrl+C) | `130` (128 + SIGINT) |

Return normally on success.

`RunAsync` returns `Task<int>`:

```csharp
int exitCode = await ApplicationBuilder.Create()
    .AddCommand<MyCommand>()
    .RunAsync(args);

Environment.Exit(exitCode);
```

Throw `CommandException` for non-zero exits:

```csharp
throw new CommandException("Configuration missing", exitCode: 2);
```

## Error Handling

The library catches `CommandException` during execution and returns its exit code. Other unhandled exceptions will propagate.

### Error Footers

Usage errors print a two-sentence `Run '...' --help` + `Run '...' --version` footer selected by error kind (`src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:28-55`, `Fault`/`NoImplementation` default at `:56-57`):

- `RequiresSubcommand` with a command name → `Run '<exe> <command-name> --help' to see available subcommands and options. Run '<exe> <command-name> --version' to show version information.`; without one (bare abstract root: message names `'<root>'` via `SubCommandInfo.DisplayName` at `SubCommandInfo.cs:32`, structured `CommandName` stays empty at `ArgumentParser.cs:95`) → the two-sentence global usage footer below. A near-miss surplus token appends a `Did you mean 'x'?` pointer via Did-You-Mean admission; a far token stays silent.
- `UnknownOption`, `MissingRequired`, `UnknownCommand`, `InvalidValue`, `DuplicateOption` with a command name → `Run '<exe> <command-name> --help' for more information on specific command options. Run '<exe> <command-name> --version' to show version information.`; without one (e.g. no command matched) → the two-sentence global usage footer below. (`DuplicateOption` is reserved and never thrown — its footer arm is kept only for compatibility.)
- `Fault`, `NoImplementation` (anything else) → single-sentence `Run '<exe> --help' for more information on available commands and options.` (no `--version` second sentence; `CommandErrorFooter.cs:56-57`).
- #509: when the failing invocation already contained `--help`/`-h`, the circular `--help` hint is suppressed and only the `--version` hint survives (e.g. `Run 'test required-test --version' to show version information.`).

`<exe>` is the configured executable name when set on the gateway path, otherwise the auto-detected one (the host path always auto-detects). Both error paths render the same footer for the same kind.

### Did-You-Mean Suggestions

At CLI dead-ends the parser appends a `Did you mean 'x'?` suggestion when the
input is close to a known name (true Damerau-Levenshtein with adjacent
transposition costing 1; case-insensitive; leading dashes stripped;
`--opt=value` compares only the part before `=`; single best match; ties break
by same-first-character prefix, then alphabetical). Admission rule: distance
≤ 2 suggests; a shared normalized first character (prefix bonus) extends
admission to distance ≤ 4; anything farther stays silent.

- Unknown option: `Unknown option: --verbosit. Did you mean '--verbosity'?`
- Unknown option with `=value` reports name-only (fail-closed logging): `--verbosit=quiet` reports `Unknown option: --verbosit. Did you mean '--verbosity'?`, never echoing the value.
- Unknown command: `No command found for 'deply'. Did you mean 'deploy'?`
- Unknown subcommand (surplus close to a child name): `Unknown subcommand 'gett'. Did you mean 'get'?`
- Unexpected extra argument (surplus far from all children): `Unexpected argument 'zzzzqqqq'` (no suggestion)

## Repeated Runs, Caching, Thread-Safety

Each `RunAsync` call builds a fresh parser and rebuilds the command topology from live registrations: type-registered commands get a fresh instance per run (bound values cannot leak across runs), instance registrations keep their identity, and late `AddCommand` / `AddCommandTypeParser` calls are visible on the next run. Per-`Type` reflection descriptors are cached per builder as immutable snapshots (double-checked lock, miss-counted by `CommandReflectionCache.BuildCount`) and reassembled into fresh per-run nodes; the per-`Type` service-injection plan (property plus optional keyed-service key, CLI-bound set hoisted in) is cached separately via its own `TypePlanCache` instance sharing the same double-checked-lock core and reused across runs; a CLI-bound/service-marked property in either cache's walk chain always throws `InvalidOperationException` (fault, exit 1) — member hiding (`new`) never excuses it; enum `FromAmong` auto-population is gated on the live parser collection for both options and positional arguments, so a custom enum parser suppresses it symmetrically. The cache layer is thread-safe (immutable descriptors with locked population plus per-run reassembly), but `ApplicationBuilder` collections, shared console output, instance-registered commands, and user command state remain caller-responsibility — do not mutate the builder or share instance registrations across concurrent runs.

## Help System

Help is automatically generated from command attributes:

```bash
myapp --help       # Global help: lists all commands
myapp deploy --help # Command-specific help: shows options & sub-commands
myapp --version    # Shows version number
```

The `--help` and `--version` flags are handled automatically — you don't need to define them. Every help screen (global and per-command) lists `-V, --version` under `GLOBAL OPTIONS:` (`src/ApplicationBuilderHelpers/CommandLineParser/HelpContentProvider.cs:105-110,220-225`): it is gateway-handled, never declared as a command option. Precedence is unchanged: completion > help > parse > version.

Help precedence (`CommandLineParser.cs:93-115`): bare help with zero collected values exits at Step 6 before validation; required validation runs at Step 7 but is skipped when `ShowHelp` is set (help always wins over missing required — `Help_Skips_Required_Validation`); binding errors collect at Step 7 through the same conversion pipeline so `InvalidValue` (exit 2) beats help-with-values; help-with-values renders at Step 7b only when the binding probe passes. Help-first-at-root carve-out (`ArgumentParser.cs:66-70`; `HelpFormatter.cs:42-44`): a leading `--help`/`-h` on the abstract root before any `--` sentinel renders the **global** model with the `COMMANDS:` section and exits `0` — so `["--help", "greet"]` and `["--help", "--bogus"]` show root help, never a command-scoped view. The `(IsRoot || argIndex > 0)` guard also sets `ShowHelp` on a known abstract parent without erroring, but only `IsRoot` maps to the global model (`HelpFormatter.cs:42-44` branches on `IsRoot` alone) — a named abstract parent keeps its parent-scoped help (`BuildCommandModel`, e.g. `["config", "hub", "--help"]` renders `"Spaced hub."`). A zero-match first token (`["bogus", "--help"]`) errors `UnknownCommand` (exit 2) before help is considered, and `["--", "--help"]` keeps `RequiresSubcommand` (exit 2). Carve-outs preserved: `--config --help` shows help (optional-bare pass skipped when `ShowHelp` is set, `ParameterValidator.cs:75`; binding collection skips bare-ledger keys, `ValueBinder.cs:46`); `--config --version` fires version (post-parse check at `CommandLineParser.cs:83-87`, before validation). Help never masks path errors (unknown option/command still exit 2). When the failing invocation already requested `--help`, the error footer keeps only the `--version` hint (the circular `--help` hint is suppressed, `CommandErrorFooter.cs:28-55`).

Reserved help-word misuse never shows help: `--help=<anything>` (including empty `--help=`), `-h=<anything>` (including empty `-h=`) unless a real short-`h` owner exists (e.g. `serve --host`, where `-h=<value>` parses as that option), bare `--no-help`, and `--no-help=<anything>` are `InvalidValue` usage errors (exit 2). Bare `--help`/`-h` still show help (exit 0).

Value placeholders (`<STRING>`, `<NUMBER>`, `<DATE>`, `<FILE>`, `<DIR>`, `<VALUE>`, `<TOKEN...>`) are documented in [Configuration & Themes](configuration.md#help-placeholder-tokens-454). Required-option markers (`(required)` ordinal, `Default:` suppression) live with help formatting in [Configuration & Themes](configuration.md#required-option-help-descriptions-507); the option-side contract is in [Commands](commands.md#required-options-in-help).

## Global Options

Define once, reference everywhere: an option declared identically on every command (same type, names, required/secret/case-sensitivity flags, environment variable, description, initializer default, and valid values) is promoted to a shared root-owned global that each command sees through the same scope view, so it behaves as one logical option everywhere within a run — a repeated scalar takes the last value, multi-value options accumulate, an explicit flag beats an environment-variable fallback (which applies only when no value was given), and names match case-sensitively (see ADR-0004).

## Tokenizer Behavior

- Bare boolean flags never consume the next token: `--verbose` binds `true` and a following word stays positional (`--verbose off` sets `Verbose: True`, `Items: off`). Use `--verbose=off` for explicit values.
- A bare valued option never consumes a flag-looking neighbor (reject-by-default): any dash-led non-numeric token — known or unknown, including `--help`/`--version` and the `--` separator — is left to bind or error on its own merits, while the valued option falls back to the trailing-bare missing sentinel (`null`, enforced in `ParameterValidator.cs:30-114`). A bare occurrence with no merged value is a missing value on its own merits — required or optional alike — and always fails `MissingRequired` (exit 2) even with env set: `Missing value for option: <display-name>` (optional) or `Missing required option: <display-name>` (required). Typing the option claims ownership, so env fallback never rescues a typed bare; it applies only to omitted (never-typed) options (`EnvVarFallback.cs:21-39`; the required rescue at `ParameterValidator.cs:29-31` is gated on no bare mark, and the optional pass at `:52-71` has no rescue call). A single trailing-bare fails even with env set; a satisfied required valued scalar repeated bare (`--name John ... --name` at end-of-line) fails `MissingRequired` (exit 2) regardless of env fallback. Precedence: an unknown neighbor errors on its own merits first — `--config --nope` reports `Unknown option: --nope` (exit 2) because the parser throws before validation runs; `--help`/`--version` neighbors keep their carve-out — `--config --help` shows help and `--config --version` fires version, since the optional-bare pass is skipped when `ShowHelp`/`ShowVersion` is set. Conversion beats help-with-values only (#483): binding errors collect at Step 7 through the same conversion pipeline (`CommandLineParser.cs:99-115`; dry-run `ValueBinder.cs:29-65`), so an `InvalidValue` (exit 2) surfaces before help-with-values; `MissingRequired` already won in the same step when `ShowHelp` is not set (under `ShowHelp`, missing is suppressed per `ParameterValidator.cs:36,98` and only the binding probe beats help). Version path untouched — the post-parse version check (`CommandLineParser.cs:83-87`) runs before validation, so invalid+version still exits `0`. Bare carve-out preserved: the collect skips bare-ledger keys when `ShowHelp` is set (`ValueBinder.cs:46`). `--config --verbose` fails `Missing value for option: -c, --config` (exit 2) whether or not `TEST_CONFIG` is set — remove the flag to use the env value. `=`-form (`--config=f.json`), compact (`-cf.json`), and numeric neighbors (`--seed -5`) still bind as values; use `--` to pass a dash-led value positionally.
- `=`-form boolean literals accept `true/false/yes/no/on/off/1/0` (case-insensitive); anything else is an `InvalidValue` usage error (exit 2), e.g. `--verbose=maybe`. On an abstract prefix with no implementation (e.g. bare root, `config` hub), a known flag in `=`-form with an invalid literal (e.g. `--verbose=banana`) reports `InvalidValue` (exit 2) naming the option plus the valid literals instead of `RequiresSubcommand` (#542 gate at `ArgumentParser.cs:473-501`, call at `:86`; literal check delegates to `SubCommandOptionInfo.ExtractValue`, no literal-table copy). Valid literals (`--verbose=true`), bare flags, valued options (`--data=x`), unknown bases (still `UnknownOption` via #508), `--no-`-prefixed tokens (owned by the `--no-` mirror), and post-`--` tokens fall through unchanged.
- Help-reserved `=`/negated forms never bind help — they are `InvalidValue` usage errors (exit 2): `--help=<anything>` (including empty `--help=`), `-h=<anything>` (including empty `-h=`) unless a real short-`h` owner exists (e.g. `serve --host`, where `-h=<value>` parses as that option), bare `--no-help`, and `--no-help=<anything>` (`ArgumentParser.cs:602-619` gate; `:629-650` error). Bare `--help`/`-h` still show help (exit 0); `--help false` shows help with `false` left positional. Post-`--` tokens stay positional and never hit this gate. On an abstract prefix (e.g. `config --help=x`) the reserved form reports `InvalidValue` (exit 2) instead of `RequiresSubcommand`.
- The first bare `--` ends option matching; every following token is positional, including `--verbose` and `--help`.
- Negative numbers (`-5`, `-1.5`) are positional without a separator — and a bare numeric token wins over a digit short: `-1`, `-10`, `-1.5` never bind a `ShortTerm '0'`–`'9'` option even when one is declared (guard at `ArgumentParser.cs:176-178`; numeric test `IsNumericValue` at `:431-444`). The digit short stays reachable only via in-token forms (`-1=value` per `SubCommandOptionInfo.cs:365,391-397`, compact `-1x` for valued options per `:369,412-415`) or after `--`.
- Combined shorts expand left to right: `-abc` binds each flag `true`; the last short takes the attached remainder (`-abdvalue` binds `Data: value`); an unknown char rejects the whole token (`Unknown option: -abx`, exit 2, `UnknownOption`). `-h`/`-V` inside a cluster win as help/version even mid-cluster. `h`/`V` are reserved shorts — declaring either as a local `ShortTerm` throws `InvalidOperationException` at registration (fail-closed, `CommandHierarchyBuilder.cs:471-497`), unless `LongName` is `help` for `-h`; `-V` always throws (no version node, gateway-only); use the long form instead (e.g. `serve --host`, long-only).
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` never accepts a value. A known name (flag, valued, or collection — including secret valued options resolved through the command's full option scope) is rejected as `InvalidValue` (exit 2) with secret-aware text that omits the value for secrets — a flag (`bool`/`bool?`) rejection advises bare `--no-<name>` (binds `false`), while a valued/collection rejection never prescribes bare `--no-<name>` (bare would itself reject as `Unknown option`) and instead advises omitting the option or using `--<name>=<value>` as a value-free template; an unknown name reports `Unknown option: --no-<name>` (exit 2) with a name-only suggestion, never echoing the value; an empty base (`--no-=value`) fails closed as `InvalidValue` (exit 2) with redaction on. On an abstract prefix (e.g. `config --no-verbose=x`) the rejected form reports `InvalidValue` (exit 2) instead of `RequiresSubcommand`.
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); valued repeats are last-wins (`--text=a --text=b` binds `b`), except a trailing bare repeat of a satisfied required valued scalar fails `MissingRequired` (exit 2) regardless of env fallback, while a trailing bare repeat of a satisfied optional is ignored and the prior value stands (`ParameterValidator.cs:64-65` merged-values gate). Unsatisfied bare (`--config` with no value anywhere) is not a repeat — it fails per the tokenizer bullet above even with env set. Bare-then-valued heals; collections accumulate; bare boolean flags stay idempotent.
