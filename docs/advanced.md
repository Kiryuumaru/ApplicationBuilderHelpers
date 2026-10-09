# Advanced Topics

Edge cases and power features: nested commands, custom hosts, exit codes, error messages, and shared options. Tokenizer rules live in [Commands](commands.md#how-typing-works). Exits live in [API Reference](api-reference.md#commandexception).

## Sub-Commands

Nest names with spaces to build hierarchies at any depth:

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
public class DeployProductionCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine("Deploying to production");
        return ValueTask.CompletedTask;
    }
}

[Command("deploy prod rollback", description: "Rollback production")]
public class DeployProductionRollbackCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine("Rolling back production");
        return ValueTask.CompletedTask;
    }
}
```

```sh
myapp deploy               # exit 0 — runs the deploy command
myapp deploy prod          # exit 0 — runs production deploy
myapp deploy prod rollback # exit 0 — runs rollback
```

When the parent has its own `Run`, bare `deploy` runs it. When the parent is only a grouping with no `Run` of its own, bare `deploy` fails with exit 2 and lists the available subcommands.

Parent help lists children: `myapp deploy --help` shows a `COMMANDS:` section with one row per child (sorted) plus a `<COMMAND> [ARGS...]` usage suffix. A flag before the child name binds at the current node only. It never looks ahead to the leaf. `myapp --config v test` binds `--config` at the root. `myapp deploy --force prod` fails as `Unknown option: --force` (exit `2`) because `deploy` owns no such flag. Globals still pass through before the child name. Type the leaf name first for leaf options: `myapp deploy prod --force` runs the leaf.

## Commands Without a Root Name

Register only leaf subcommands and your app has no root command of its own. Running it bare fails with exit 2 and lists subcommands. `--help` still works: `--help greet` shows `greet` help, and bare `--help` shows global help.

Give the root a description-only `[Command]` and it becomes a real command again with its own behavior.

## Custom Host Types

Console apps are the default. Extend plain `Command` and you get a `HostApplicationBuilder` host:

```csharp
public class MyCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine("Running");
        return ValueTask.CompletedTask;
    }
}
```

Need a server? Extend `Command<T>` with your host builder type and build it yourself:

```csharp
[Command("serve", description: "Serve HTTP traffic")]
public class WebCommand : Command<WebApplicationBuilder>
{
    protected override ValueTask<WebApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = WebApplication.CreateBuilder();
        return new ValueTask<WebApplicationBuilder>(builder);
    }

    protected override ValueTask Run(ApplicationHost<WebApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        var app = applicationHost.Builder.Build();
        app.MapGet("/", () => "Hello World");
        return new ValueTask(app.RunAsync(cancellationToken));
    }
}
```

The web sample needs ASP.NET Core packages.

Custom `Command<T>` hosts keep their own logging. The default sink policy below does not apply to them.

## Exit Codes

| Outcome | Exit code | Stream |
|---|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` | stdout |
| Usage error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` only with `SetRejectDuplicateOptions(true)` — repeats otherwise take the last value) | `2` (duplicate errors list first, then missing, then invalid-value errors; missing-only keeps kind `MissingRequired`, duplicate-only keeps kind `DuplicateOption`, any invalid line makes the kind `InvalidValue`) | stderr |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` | stderr |
| Cancellation (`CancellationToken` / Ctrl+C / SIGTERM) | `130` | none (shutdown diagnostics use stderr) |

`RunAsync` returns the exit code as `Task<int>`:

```csharp
int exitCode = await ApplicationBuilder.Create()
    .AddCommand<MyCommand>()
    .RunAsync(args);

Environment.Exit(exitCode);
```

Return a custom exit from `Run` by throwing `CommandException`:

```csharp
throw new CommandException("Configuration missing", exitCode: 2);
```

## Error Handling

The library catches `CommandException` and returns its exit code. Other unhandled errors propagate. Usage errors and faults print to stderr.

### Error Footers

Usage errors print a help hint plus a global version hint:

- `RequiresSubcommand` with a command name points at that command's help; without one it points at global help. The version hint stays global.
- `UnknownOption`, `MissingRequired`, `UnknownCommand`, `InvalidValue`, `DuplicateOption` with a command name point at that command's help; without one they point at global help. The version hint stays global.
- `Fault`, `NoImplementation` print only the `--help` hint.
- When the failing call already contained `--help`/`-h`, only the `--version` hint survives. Parser and host threads agree here.

### Did-You-Mean Suggestions

Near-misses get a pointer. Far misses stay silent:

- `Unknown option: --verbosit. Did you mean '--verbosity'?`
- `No command found for 'deply'. Did you mean 'deploy'?`
- `Unknown subcommand 'gett'. Did you mean 'get'?`

Fail-closed rules: an identical token never suggests itself, ambiguous
ties stay silent with the plain unknown-token error (exit `2`), and
same-first-letter matches at distance 3-4 only suggest for long tokens.
Cluster fragments report the failing char only (`Unknown option: -z`).

A concrete root with its own run reports any leading bare non-child word as `No command found` (exit `2`), with a pointer only when close; only a childless root binds a bare word positionally. A bare miss behind leading help tokens reports `No command found` before forwarding; only a hit forwards to target help. A named grouping parent keeps `Unknown subcommand` wording for a near miss behind its help token, while a far miss keeps the subcommand list.

Reserved `--help`/`--version` always compete in option ranking: `--versoin` suggests
`--version`, and `--ver` suggests `--version` over `--verbose` on an exact
distance + same-initial tie. A strictly closer user option still wins.

## Host Logging

Plain `Command` apps keep stdout clean by default. Status messages stay silent. Console logs write to stderr.

To change logging, override `BuilderPreparation` or `AddServices`. Those hooks run after the default policy, so your settings win:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
{
    applicationBuilder.Services.Configure<ConsoleLifetimeOptions>(
        options => options.SuppressStatusMessages = false);
}

public override void AddServices(
    ApplicationHostBuilder applicationBuilder,
    IServiceCollection services)
{
    services.Configure<ConsoleLoggerOptions>(
        options => options.LogToStandardErrorThreshold = LogLevel.None);
}
```

`SuppressStatusMessages = false` restores status lines. `LogToStandardErrorThreshold` picks which levels use stderr.

## Running Twice and Thread Safety

Each `RunAsync` call rebuilds from your live registrations. Commands registered by type get a fresh instance per run, so values never leak between runs. Commands registered by instance keep their identity. Calls to `AddCommand` or `AddCommandTypeParser` between runs apply on the next run.

You can call `RunAsync` again on the same builder. Never mutate the builder or share instance registrations across concurrent runs.

## Help System

Help generates automatically from your command attributes:

```sh
myapp --help        # Global help: lists all commands
myapp deploy --help # Command-specific help: shows options and sub-commands
myapp --version     # Shows version number
```

You never define `--help` or `--version`. Help flags are `--help`/`-h`, plus the `-?` and `/?` aliases (bare token only, every OS; `?` never expands inside a cluster). Version flags are `--version`/`-V`.

On Unix shells quote the `?` aliases (`'-?'`, `'/?'`) so the shell does not glob them.

One gate decides help/version forgiveness before any validation:

1. Bare `--help` (or `-h`, `-?`, `/?`) shows help before any checks.
2. Help or version with its flag present skips all validation (so `--help` beats a missing required option); deferred conversion errors stay forgiven kind-blind, except a dangling valued option beside help still fails (exit `2`).
3. Help beats version when both appear.
4. Unknown, misuse, requires-subcommand, and invalid-literal errors still beat both.

A dangling valued option (bare `--config` with no value beside help) clears help and exits `2`; a typed bare scalar fails even with its environment variable set — env rescues only omitted valued options, and collections with merged values stay exempt. An empty `=`-form on a non-string option fails as `InvalidValue` (exit `2`); string options still bind empty (and satisfy `Required`; reject emptiness in code when you need non-empty). Other `=`-form value errors stay forgiven beside help. `-help` and `-version` are exact-only full-token unknown options with a did-you-mean pointer, never clusters; neither requests help or version, so both footer hints survive. Global help with no subcommands prints no `<command>` footer.

Help never hides typos. `bogus --help` reports an unknown command (exit 2). On a concrete root with its own run, a miss behind leading help reports `No command found` before forwarding and only a hit forwards to target help. A named grouping parent reports a near miss behind its help token as `Unknown subcommand` with a pointer, while a far miss keeps the subcommand list. A surplus bare word after a resolved help path shows leaf help (exit `0`) when the resolved node owns a positional slot (`config --help get extra` shows `get` help because `get` owns `[key]`), and errors as `Unexpected argument` (exit `2`) when it owns none (`--help config extra` fails because `config` offers no slot). `--help=<anything>` fails as `does not accept a value` (`InvalidValue`, exit 2), never help, and `-h=<anything>`, `-?=<anything>` / `/?=<anything>`, `--version=<anything>`, `-V=<anything>` fail the same way with their own token named. `--` ends option matching and blocks help: `-- --help` stays exit 2, and `-- -?` / `-- /?` stay positional.

Value placeholders (`<STRING>`, `<NUMBER>`, `<DATE>`, `<FILE>`, `<DIR>`, `<VALUE>`) live in [Configuration](configuration.md#help-placeholders). Required markers live in [Configuration](configuration.md#required-options-in-help).

## Global Options

Declare an option identically on every command and it becomes one shared global: one value in every scope. A repeated scalar takes the last value. Turn on strict mode with `SetRejectDuplicateOptions(true)` and a repeated scalar valued option fails as `DuplicateOption` (exit 2, `Duplicate option: <display-name>`). Collections stay exempt and accumulate. Flags stay exempt. Values from the environment stay exempt. Flags are bare-only and never consult the environment-variable fallback; env covers omitted valued options only. Option names always match exactly (`Ordinal`). `CaseSensitive` affects `FromAmong` values only; it never affects names. Type a prefix or shorthand and the parser reports `Unknown option` (exit `2`).

## Tokenizer Behavior

The full typing rules live in [Commands](commands.md#how-typing-works). Short version:

- Bare flags never eat the next word. A flag with `=` never works (exit `2`).
- A bare valued option never steals a flag-looking word. A trailing bare repeat of a valued scalar fails as missing (exit `2`), even with env set or a prior value. Env covers omitted valued options only. Flags are bare-only and never consult env.
- One splitter parses response files and completion prefixes. It strips `"` and `'` quotes. Any whitespace splits, including newlines. Files have no comments. Response-file rules and limits live in [Commands](commands.md#response-files-file).
