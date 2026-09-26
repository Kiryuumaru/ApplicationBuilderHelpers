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

### Abstract root

Register only leaf subcommands (no root implementation) and the root stays abstract. It has no implementation of its own.

- Run bare (`[]`) and you get exit `2`: `'<root>' requires a subcommand. Available subcommands: ...`. No `Did you mean` pointer appears. There is nothing to match.
- Put `--help` first and help wins. Run `--help greet` and you see root help, never `greet` help. Run `--help --bogus` and you still see root help. The unknown flag is ignored on this path.
- Only the root shows the **global** help model with the `COMMANDS:` section. A named parent keeps its own scoped view. Run `config hub --help` and you see `"Spaced hub."`.

| Input | Exit | Result |
|---|---|---|
| `[]` | `2` | `'<root>' requires a subcommand` + global footer |
| `["--help", "greet"]` | `0` | Root/global help, never `greet` help |
| `["--help", "--bogus"]` | `0` | Root/global help (unknown trailing flag ignored) |
| `["bogus", "--help"]` | `2` | `No command found for 'bogus'` (zero-match wins before help) |
| `["--", "--help"]` | `2` | `'<root>' requires a subcommand` (the `--` sentinel blocks help) |
| `["config", "hub", "--help"]` | `0` | Parent-scoped help (`"Spaced hub."`) — globalization is root-only |
| `["--verbose=banana"]` | `2` | `Invalid Boolean value 'banana' for option '--verbose'` (#542 beats `RequiresSubcommand` on a root-visible flag) |
| `["--verbose=true"]` | `2` | `'<root>' requires a subcommand` (valid literal falls through) |
| `["--", "--verbose=banana"]` | `2` | `'<root>' requires a subcommand` (the `--` sentinel blocks the #542 gate) |

Pinned by `SubCommandInfo.IsRoot` (`SubCommandInfo.cs:128`), display name `"<root>"` (`:32`), empty structured `CommandName` so the footer stays global (`CommandErrorFooter.cs`); abstract help-first branch with the #558 surplus-path guard (`ArgumentParser.cs:47-60`); `HelpFormatter` branches on `IsRoot` alone (`HelpFormatter.cs:40-42`); `AbstractRootRequiresSubcommandTests.cs` (13 tests) + `AbstractRootFlagLiteralTests.cs` (17 tests).

### Concrete root (#559)

Give the root its own description-only `[Command]` and it becomes concrete. The abstract branch above never fires. A leading bare `--help`/`-h` still shows global help with exit `0`. Trailing tokens are never validated.

| Input (concrete root) | Exit | Result |
|---|---|---|
| `["--help", "false"]` | `0` | Root/global help — trailing value never validated (never reaches `Run`) |
| `["--help", "test"]` | `0` | Root/global help — never `test` help (help-first wins, no routing into `test`); the global `COMMANDS:` list legitimately names `test` |
| `["--help", "--bogus"]` | `0` | Root/global help (unknown trailing flag ignored on the help-first path) |
| `["-h", "false"]` | `0` | Root/global help (`-h` is a help token; `-?` is not — `["-?", "false"]` stays `Unknown option`, exit `2`) |
| `["--help", "--help=x"]` / `["--help", "--no-help"]` / `["--help", "--", "--bogus"]` | `0` | Root/global help (trailing tokens ignored once the leading-help gate fires) |
| `["--help", "--version"]` / `["--help", "-V"]` | `0` | Version text (version beats help) |
| `["--bogus", "--help"]` | `2` | `Unknown option: --bogus` (non-leading help never fires the gate; #509 keeps only the `--version` hint) |
| `["--help=x"]` / `["--helpful"]` | `2` | `InvalidValue` / `Unknown option` (reserved `=`-form and lookalikes are not bare help tokens) |

Watch the edges. Run `-- --help` and you still get exit `2`: the sentinel blocks the gate. Run `--help --verbose=banana` and you get help (exit `0`): the #542 gate lives inside the abstract branch and never runs here. Run bare `--help` and you get exit `0` through the pre-parse global-help shortcut.

Pinned by `IsConcreteRootLeadingHelp` (`ArgumentParser.cs:530-537`: root-only, leading bare `--help`/`-h`, no pre-`--` version token; separate block at `:70-73` after the abstract help and version checks) and `HelpVersionGateway.IsHelpToken` (`--help`/`-h` only); `RootRoutingDivergenceTests.cs` (4 tests: `ConcreteRoot_Help_WithTrailingValue_ShowsRootHelp`, `ConcreteRoot_Help_WithTrailingCommandName_ShowsRootHelp`, `ConcreteRoot_Help_WithTrailingUnknownOption_ShowsRootHelp`, `Leaf_Help_WithTrailingValue_ShowsLeafHelp`).

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
| `Run` returns normally (also `--help` / `--version`) | `0` (conversion failure beats help-with-values; invalid+version still `0` via the pre-validation version guard; leading `--help`/`-h` on a concrete root renders global help, exit `0`, before trailing validation) |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` is reserved and never thrown — valued repeats resolve last-wins) | `2` (missing errors list first, then invalid-value errors, joined with newlines; missing-only keeps kind `MissingRequired`, any invalid line makes the kind `InvalidValue`) |
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

Usage errors print a two-sentence `Run '...' --help` + `Run '...' --version` footer selected by error kind:

- `RequiresSubcommand` with a command name → `Run '<exe> <command-name> --help' to see available subcommands and options. Run '<exe> <command-name> --version' to show version information.`; without one (bare abstract root names `'<root>'` but the structured `CommandName` stays empty) → the two-sentence global usage footer below. A near-miss surplus token appends a `Did you mean 'x'?` pointer; a far token stays silent.
- `UnknownOption`, `MissingRequired`, `UnknownCommand`, `InvalidValue`, `DuplicateOption` with a command name → `Run '<exe> <command-name> --help' for more information on specific command options. Run '<exe> <command-name> --version' to show version information.`; without one (e.g. no command matched) → the two-sentence global usage footer below. (`DuplicateOption` is reserved and never thrown — its footer arm is kept only for compatibility.)
- `Fault`, `NoImplementation` (anything else) → single-sentence `Run '<exe> --help' for more information on available commands and options.` (no `--version` second sentence).
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

Each `RunAsync` call builds a fresh parser from live registrations. Type-registered commands get a fresh instance per run, so bound values cannot leak across runs. Instance registrations keep their identity. Late `AddCommand` / `AddCommandTypeParser` calls show up on the next run.

Per-`Type` reflection descriptors are cached per builder as immutable snapshots and reassembled into fresh per-run nodes. The per-`Type` service-injection plan is cached separately and reused across runs. A CLI-bound/service-marked property in either cache's walk chain always throws `InvalidOperationException` (fault, exit 1) — member hiding (`new`) never excuses it. Enum `FromAmong` auto-population follows the live parser collection for options and positional arguments alike, so a custom enum parser suppresses it on both sides.

The cache layer is thread-safe. `ApplicationBuilder` collections, shared console output, instance-registered commands, and user command state are not. Do not mutate the builder or share instance registrations across concurrent runs.

## Help System

Help is automatically generated from command attributes:

```bash
myapp --help       # Global help: lists all commands
myapp deploy --help # Command-specific help: shows options & sub-commands
myapp --version    # Shows version number
```

The `--help` and `--version` flags are handled automatically — you don't need to define them. Every help screen (global and per-command) lists `-V, --version` under `GLOBAL OPTIONS:`. It is gateway-handled, never declared as a command option. Precedence is unchanged: completion > help > parse > version.

Help precedence has four rules. First, bare help exits before validation. Run `--help` with no values and help renders. Second, help beats missing required. Run `--help` with a missing required option and help still renders. Third, conversion errors beat help-with-values. Pass a bad value with `--help` and you get `InvalidValue` (exit 2). Help-with-values renders only when the binding probe passes.

Help-first at root is a carve-out. Put `--help`/`-h` first at the root before any `--` and the global model renders with exit `0`. Run `--help greet` or `--help --bogus` and you see root help. The concrete root (#559) uses the same carve-out through its own block after the abstract help and version checks. Run `--help false`, `--help test`, or `--help --bogus` on a concrete root and you see root help (exit `0`). A pre-`--` version token still wins: `--help --version` prints version. `-?` is not a help token.

Help never hides typos. Run `bogus --help` and you get `UnknownCommand` (exit 2). Run `config gett --help` and you still fail with exit 2. A near-miss adds a suggestion (`Did you mean 'get'?`, #558) — same as a mistyped top-level command. Run `-- --help` and the sentinel blocks help: abstract root keeps `RequiresSubcommand` (exit 2), concrete root fails the post-`--` token as surplus (exit 2).

Carve-outs stay intact. Run `--config --help` and help shows. Run `--config --version` and version fires. When the failing call already contained `--help`, the footer keeps only the `--version` hint.

Pinned by `ValidateAndBindParameters` (missing skipped when `ShowHelp` is set; help-with-values renders only when the binding probe passes); abstract help-first with the #558 surplus-path guard plus `HelpFormatter` branching on `IsRoot` alone (named parent keeps `BuildCommandModel`); concrete-root `IsConcreteRootLeadingHelp` (root-only, leading bare token, pre-`--` version yields). Sources: `CommandLineParser.cs`, `ArgumentParser.cs:47-74`, `HelpFormatter.cs:40-42`, `HelpVersionGateway.cs`, `ParameterValidator.cs`, `ValueBinder.cs`, `CommandErrorFooter.cs`.

Reserved help-word misuse never shows help: `--help=<anything>` (including empty `--help=`), `-h=<anything>` (including empty `-h=`) unless a real short-`h` owner exists (e.g. `serve --host`, where `-h=<value>` parses as that option), bare `--no-help`, and `--no-help=<anything>` are `InvalidValue` usage errors (exit 2). Bare `--help`/`-h` still show help (exit 0).

Value placeholders (`<STRING>`, `<NUMBER>`, `<DATE>`, `<FILE>`, `<DIR>`, `<VALUE>`, `<TOKEN...>`) are documented in [Configuration & Themes](configuration.md#help-placeholder-tokens-454). Required-option markers (`(required)` ordinal, `Default:` suppression) live with help formatting in [Configuration & Themes](configuration.md#required-option-help-descriptions-507); the option-side contract is in [Commands](commands.md#required-options-in-help).

## Global Options

Define once, reference everywhere: declare an option identically on every command and it becomes one shared root-owned global. It behaves as one logical option everywhere within a run. A repeated scalar takes the last value. Multi-value options accumulate. An explicit flag beats an environment-variable fallback, which applies only when you omit the option. Names match case-sensitively (see ADR-0004).

## Tokenizer Behavior

- Bare boolean flags never consume the next token: `--verbose` binds `true` and a following word stays positional (`--verbose off` sets `Verbose: True`, `Items: off`). Use `--verbose=off` for explicit values.
- A bare valued option never steals a flag-looking neighbor. Run `--config --nope` and you get `Unknown option: --nope` (exit 2). Run `--config --help` and help shows. Run `--config --version` and version fires. Type a valued option with no value and it fails `MissingRequired` (exit 2), even with env set. Env fallback only covers omitted options, never typed ones. Run `--config --verbose` and you get `Missing value for option: -c, --config` whether or not `TEST_CONFIG` is set. Remove the flag to use the env value. Use `=`-form (`--config=f.json`), compact (`-cf.json`), or numeric neighbors (`--seed -5`) to bind. Use `--` to pass a dash-led value positionally. Conversion errors (#483) beat help-with-values; missing is skipped when help shows; invalid+version still exits `0`.
- `=`-form boolean literals accept `true/false/yes/no/on/off/1/0` (case-insensitive); anything else is an `InvalidValue` usage error (exit 2), e.g. `--verbose=maybe`. On an abstract prefix with no implementation (bare root, `config` hub), an invalid literal (e.g. `--verbose=banana`) reports `InvalidValue` naming the option instead of `RequiresSubcommand` (#542). Valid literals, bare flags, valued options, unknown bases (still `UnknownOption`), `--no-` tokens, and post-`--` tokens fall through unchanged. A leading `--help` on a concrete root never reaches the #542 gate: the #559 block runs first, so `--help --verbose=banana` renders help (exit `0`).
- Help-reserved `=`/negated forms never bind help — they are `InvalidValue` usage errors (exit 2): `--help=<anything>` (including empty `--help=`), `-h=<anything>` (including empty `-h=`) unless a real short-`h` owner exists (e.g. `serve --host`, where `-h=<value>` parses as that option), bare `--no-help`, and `--no-help=<anything>`. Bare `--help`/`-h` still show help (exit 0); `--help false` shows help with `false` left positional (concrete root via the #559 gate; abstract root via help-first). Post-`--` tokens stay positional. On an abstract prefix (e.g. `config --help=x`) the reserved form reports `InvalidValue` instead of `RequiresSubcommand`.
- The first bare `--` ends option matching; every following token is positional, including `--verbose` and `--help`. The #559 gate scans only pre-`--` tokens and needs `argIndex == 0`, so `-- --help` never fires it (abstract root: `RequiresSubcommand`, exit `2`; concrete root: surplus-argument error, exit `2`).
- Negative numbers (`-5`, `-1.5`) are positional without a separator. A bare numeric token wins over a digit short: `-1`, `-10`, `-1.5` never bind a `ShortTerm '0'`–`'9'` option even when one is declared. Reach the digit short only via in-token forms (`-1=value`, compact `-1x` for valued options) or after `--`.
- Combined shorts expand left to right: `-abc` binds each flag `true`; the last short takes the attached remainder (`-abdvalue` binds `Data: value`); an unknown char rejects the whole token (`Unknown option: -abx`, exit 2). `-h`/`-V` inside a cluster win as help/version even mid-cluster. `h`/`V` are reserved shorts — declaring either as a local `ShortTerm` throws `InvalidOperationException` at registration, unless `LongName` is `help` for `-h`; `-V` always throws (no version node, gateway-only); use the long form instead (e.g. `serve --host`, long-only).
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` never accepts a value. A known name (flag, valued, or collection — including secret valued options resolved through the command's full option scope) is rejected as `InvalidValue` (exit 2) with secret-aware text that omits the value for secrets — a flag (`bool`/`bool?`) rejection advises bare `--no-<name>` (binds `false`), while a valued/collection rejection never prescribes bare `--no-<name>` (bare would itself reject as `Unknown option`) and instead advises omitting the option or using `--<name>=<value>` as a value-free template; an unknown name reports `Unknown option: --no-<name>` (exit 2) with a name-only suggestion, never echoing the value; an empty base (`--no-=value`) fails closed as `InvalidValue` (exit 2) with redaction on. On an abstract prefix (e.g. `config --no-verbose=x`) the rejected form reports `InvalidValue` (exit 2) instead of `RequiresSubcommand`.
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); valued repeats are last-wins (`--text=a --text=b` binds `b`). A trailing bare repeat of a satisfied required valued scalar fails `MissingRequired` (exit 2) regardless of env fallback, while a trailing bare repeat of a satisfied optional is ignored and the prior value stands. Unsatisfied bare (`--config` with no value anywhere) is not a repeat — it fails per the tokenizer bullet above even with env set. Bare-then-valued heals; collections accumulate; bare boolean flags stay idempotent.

Tokenizer sources: `ParameterValidator.cs`, `EnvVarFallback.cs`, `ValueBinder.cs`, `CommandLineParser.cs`, `ArgumentParser.cs`, `SubCommandOptionInfo.cs`, `CommandHierarchyBuilder.cs`.
