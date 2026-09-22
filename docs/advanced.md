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
| `Run` returns normally (also `--help` / `--version`) | `0` |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`, `DuplicateOption`) | `2` |
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

Usage errors print a `Run '...' --help` footer selected by error kind:

- `RequiresSubcommand` with a command name → `Run '<exe> <command-name> --help' to see available subcommands and options.`; without one → the global footer below. A near-miss surplus token appends a `Did you mean 'x'?` pointer via Did-You-Mean admission; a far token stays silent.
- `UnknownOption`, `MissingRequired`, `UnknownCommand`, `InvalidValue`, `DuplicateOption` with a command name → `Run '<exe> <command-name> --help' for more information on specific command options.`; without one (e.g. no command matched) → the global footer below.
- Anything else → `Run '<exe> --help' for more information on available commands and options.`

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

Each `RunAsync` call builds a fresh parser and rebuilds the command topology from live registrations: type-registered commands get a fresh instance per run (bound values cannot leak across runs), instance registrations keep their identity, and late `AddCommand` / `AddCommandTypeParser` calls are visible on the next run. Per-`Type` reflection descriptors are cached per builder as immutable snapshots and reassembled into fresh per-run nodes; enum `FromAmong` auto-population is gated on the live parser collection for both options and positional arguments, so a custom enum parser suppresses it symmetrically. The cache layer is thread-safe (immutable descriptors with locked population plus per-run reassembly), but `ApplicationBuilder` collections, shared console output, instance-registered commands, and user command state remain caller-responsibility — do not mutate the builder or share instance registrations across concurrent runs.

## Help System

Help is automatically generated from command attributes:

```bash
myapp --help       # Global help: lists all commands
myapp deploy --help # Command-specific help: shows options & sub-commands
myapp --version    # Shows version number
```

The `--help` and `--version` flags are handled automatically — you don't need to define them.

Value placeholders (`<STRING>`, `<NUMBER>`, `<DATE>`, `<FILE>`, `<DIR>`, `<VALUE>`, `<TOKEN...>`) are documented in [Configuration & Themes](configuration.md#help-placeholder-tokens-454).

## Global Options

Define once, reference everywhere: an option declared identically on every command (same type, names, required/secret/case-sensitivity flags, environment variable, description, initializer default, and valid values) is promoted to a shared root-owned global that each command sees through the same scope view, so it behaves as one logical option everywhere within a run — a repeated scalar takes the last value, multi-value options accumulate, an explicit flag beats an environment-variable fallback (which applies only when no value was given), and names match case-sensitively (see ADR-0004).

## Tokenizer Behavior

- Bare boolean flags never consume the next token: `--verbose` binds `true` and a following word stays positional (`--verbose off` sets `Verbose: True`, `Items: off`). Use `--verbose=off` for explicit values.
- A bare valued option never consumes a flag-looking neighbor (reject-by-default): any dash-led non-numeric token — known or unknown, including `--help`/`--version` and the `--` separator — is left to bind or error on its own merits, while the valued option falls back to the trailing-bare missing sentinel (`null`, enforced in `ParameterValidator.cs:20-71`). A bare occurrence with no merged value is a missing value on its own merits — required or optional alike — and always fails `MissingRequired` (exit 2) even with env set: `Missing value for option: <display-name>` (optional) or `Missing required option: <display-name>` (required). Typing the option claims ownership, so env fallback never rescues a typed bare; it applies only to omitted (never-typed) options (`EnvVarFallback.cs:21-39`; the required rescue at `ParameterValidator.cs:29-31` is gated on no bare mark, and the optional pass at `:52-71` has no rescue call). A single trailing-bare fails even with env set; a satisfied required valued scalar repeated bare (`--name John ... --name` at end-of-line) fails `MissingRequired` (exit 2) regardless of env fallback. Precedence: an unknown neighbor errors on its own merits first — `--config --nope` reports `Unknown option: --nope` (exit 2) because the parser throws before validation runs; `--help`/`--version` neighbors keep their carve-out — `--config --help` shows help and `--config --version` fires version, since the optional-bare pass is skipped when `ShowHelp`/`ShowVersion` is set. `--config --verbose` fails `Missing value for option: -c, --config` (exit 2) whether or not `TEST_CONFIG` is set — remove the flag to use the env value. `=`-form (`--config=f.json`), compact (`-cf.json`), and numeric neighbors (`--seed -5`) still bind as values; use `--` to pass a dash-led value positionally.
- `=`-form boolean literals accept `true/false/yes/no/on/off/1/0` (case-insensitive); anything else is an `InvalidValue` usage error (exit 2), e.g. `--verbose=maybe`.
- The first bare `--` ends option matching; every following token is positional, including `--verbose` and `--help`.
- Negative numbers (`-5`, `-1.5`) are positional without a separator.
- Combined shorts expand left to right: `-abc` binds each flag `true`; the last short takes the attached remainder (`-abdvalue` binds `Data: value`); an unknown char rejects the whole token (`Unknown option: -abx`, exit 2, `UnknownOption`). `-h`/`-V` inside a cluster win as help/version even mid-cluster.
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` never accepts a value. A known name (flag, valued, or collection — including secret valued options resolved through the command's full option scope) is rejected as `InvalidValue` (exit 2) with secret-aware text that omits the value for secrets; an unknown name reports `Unknown option: --no-<name>` (exit 2) with a name-only suggestion, never echoing the value; an empty base (`--no-=value`) fails closed as `InvalidValue` (exit 2) with redaction on.
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); valued repeats are last-wins (`--text=a --text=b` binds `b`), except a trailing bare repeat of a satisfied required valued scalar fails `MissingRequired` (exit 2) regardless of env fallback, while a trailing bare repeat of a satisfied optional is ignored and the prior value stands (`ParameterValidator.cs:64-65` merged-values gate). Unsatisfied bare (`--config` with no value anywhere) is not a repeat — it fails per the tokenizer bullet above even with env set. Bare-then-valued heals; collections accumulate; bare boolean flags stay idempotent.
