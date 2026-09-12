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

### Did-You-Mean Suggestions

At CLI dead-ends the parser appends a `Did you mean 'x'?` suggestion when the
input is close to a known name (true Damerau-Levenshtein with adjacent
transposition costing 1; case-insensitive; leading dashes stripped;
`--opt=value` compares only the part before `=`; single best match; ties break
by same-first-character prefix, then alphabetical). Admission rule: distance
≤ 2 suggests; a shared normalized first character (prefix bonus) extends
admission to distance ≤ 4; anything farther stays silent.

- Unknown option: `Unknown option: --verbosit. Did you mean '--verbosity'?`
- Unknown command: `No command found for 'deply'. Did you mean 'deploy'?`
- Unknown subcommand (surplus close to a child name): `Unknown subcommand 'gett'. Did you mean 'get'?`
- Unexpected extra argument (surplus far from all children): `Unexpected argument 'zzzzqqqq'` (no suggestion)

## Help System

Help is automatically generated from command attributes:

```bash
myapp --help       # Global help: lists all commands
myapp deploy --help # Command-specific help: shows options & sub-commands
myapp --version    # Shows version number
```

The `--help` and `--version` flags are handled automatically — you don't need to define them.

## Tokenizer Behavior

- Bare boolean flags never consume the next token: `--verbose` binds `true` and a following word stays positional (`--verbose off` sets `Verbose: True`, `Items: off`). Use `--verbose=off` for explicit values.
- `=`-form boolean literals accept `true/false/yes/no/on/off/1/0` (case-insensitive); anything else is an `InvalidValue` usage error (exit 2), e.g. `--verbose=maybe`.
- The first bare `--` ends option matching; every following token is positional, including `--verbose` and `--help`.
- Negative numbers (`-5`, `-1.5`) are positional without a separator.
- Combined shorts expand left to right: `-abc` binds each flag `true`; the last short takes the attached remainder (`-abdvalue` binds `Data: value`); an unknown char rejects the whole token (`Unknown option: -abx`, exit 2, `UnknownOption`). `-h`/`-V` inside a cluster win as help/version even mid-cluster.
- `--no-<name>` negates a boolean flag (`--no-verbose` binds `false`); `--no-<name>=value` is rejected as `InvalidValue` (exit 2). Unknown names report `Unknown option` (exit 2).
- Bare-flag repetition is idempotent (`--verbose --verbose` succeeds); a valued repeat is a `DuplicateOption` usage error (exit 2).
