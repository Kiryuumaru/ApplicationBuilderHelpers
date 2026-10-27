# Advanced Topics

Edge cases and power features: nested commands, custom hosts, exit codes, error messages, and shared options. Tokenizer rules live in [Commands](commands.md#how-typing-works). Exits live in [API Reference](api-reference.md#handle-errors-and-exit-codes).

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

Registration order does not affect the sub-command hierarchy: registering a parent after its sub-commands keeps them.

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

Custom `Command<T>` hosts keep their own logging. The default policy below still applies to them: status messages are suppressed, and `Microsoft.Hosting.Lifetime` is filtered to `Warning` and above. Stream routing stays yours — the plain `Command` base is what sends console logs to stderr; custom hosts keep the console logger's default (stdout).

## Exit Codes

| Outcome | Exit code | Stream |
|---|---|---|
| `Run` returns normally (without a live external cancellation; also `--help` / `--version`) | `0` | stdout |
| Usage error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` only with `SetRejectDuplicateOptions(true)` — repeats otherwise take the last value) | `2` (duplicate errors list first, then missing, then invalid-value errors; missing-only keeps kind `MissingRequired`, duplicate-only keeps kind `DuplicateOption`, any invalid line makes the kind `InvalidValue`) | stderr |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` | stderr |
| Cancellation (`CancellationToken` / Ctrl+C / SIGTERM), including during post-success lifetime cleanup | `130` | none (shutdown diagnostics use stderr by default in plain `Command` apps) |

**Cancellation precedence.** External cancellation wins whenever it is live as the command returns or the host exits `0`: a pre-canceled outer token, Ctrl+C, or SIGTERM makes a command that returns normally exit `130`. Without an external cancellation request, an `OperationCanceledException` or internal cancellation exits `0`. Help, version, and completion answers stay `0`.

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

The library catches `CommandException` and returns its exit code. Other errors thrown while a command runs do not propagate to the caller of `RunAsync`. Errors raised outside the run pipeline (builder configuration calls, e.g. `SetHelpWidth`) are not caught and surface raw. Usage errors and faults print to stderr.

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

To change logging, override `BuilderPreparation` or `AddServices`. Those hooks run after the default policy, so your settings win. A filter override must target the default filter's category (`Microsoft.Hosting.Lifetime`) to displace it; a category-less or broader filter cannot:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
{
    applicationBuilder.Services.Configure<ConsoleLifetimeOptions>(
        options => options.SuppressStatusMessages = false);
    applicationBuilder.Builder.Logging.AddFilter(
        "Microsoft.Hosting.Lifetime", LogLevel.Information);
}

public override void AddServices(
    ApplicationHostBuilder applicationBuilder,
    IServiceCollection services)
{
    services.Configure<ConsoleLoggerOptions>(
        options => options.LogToStandardErrorThreshold = LogLevel.None);
}
```

The default policy sets two gates: `SuppressStatusMessages = true` and a `Microsoft.Hosting.Lifetime` filter at `Warning`. Relax both to restore status lines. `SuppressStatusMessages = false` alone restores nothing — the filter still blocks the `Information` status lines. `LogToStandardErrorThreshold` picks which levels use stderr.

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

One gate decides help/version forgiveness before parameter and value validation:

1. Bare `--help` (or `-h`, `-?`, `/?`) shows help before any checks.
2. Help or version with a live request skips parameter and value validation (duplicate-option errors under strict mode, missing-required inputs, and binding/conversion errors), so `--help` beats a missing required option; structural path errors still beat both (rule 4). Deferred conversion errors stay forgiven kind-blind. A dangling valued option beside leaf help still clears help and exits `2` when no version token is involved: `config get --format --help` reports `Missing value for option: --format`. If an exact `--version`/`-V` token appears before the leaf path and the parser skips it on the grouping path, the request stays available to leaf validation and forgiveness; help wins, and the resolved leaf help is shown instead.
3. Help beats version once a valid help path resolves. Help-target resolution and surplus classification skip exact `--version`/`-V` tokens anywhere in the live help path, including after the leaf name. The path-walk prefix skip is narrower: it happens only on a grouping node, before the leaf is reached. On a grouping node (a named grouping parent or an unnamed abstract root), the path-walk prefix skip applies to an exact `--version`/`-V` token that co-occurs with a live help token before `--`, in either order. It does not hide a later child, miss, or surplus. The guarantee covers exactly those reserved tokens. Any other dash-led token remains grammar-dependent, so option-interleaved inputs stay outside the guarantee. An after-leaf version token is still inside the help-target rule: `config --help get --version extra` and `config --help get -V extra` exit `2` with `Unexpected argument 'extra'`, matching `config --help get extra`; `config --help get --version` and `config --help get -V` show `get` help, matching `config --help get`. Concrete-root leading help keeps its existing behavior. On a grouping parent, a version-only request still wins even with a bare surplus (`config --version bogus` prints the version, exit `0`).
4. Unknown, misuse, requires-subcommand, and invalid-literal errors still beat both. On a named grouping parent, a mistyped subcommand followed by both `--help` and `--version` reports `RequiresSubcommand` and exits `2` (`config bogus --help --version`, `config bogus --version --help`), matching `config bogus --help`.

On a command with its own run, a dangling valued option beside help clears help and exits `2` when no version token is involved: `config get --format --help` reports `Missing value for option: --format`. If an exact `--version`/`-V` token appears before the leaf path and was skipped on the grouping path, that request keeps the leaf help live. `config --version get --format --help` and `config -V get --format --help` exit `0`, write the same stdout as `config get --help`, and leave stderr empty. The page is `config get` leaf help, never group help.

A required valued option on a command with its own run consumes a following bare help token (`--help`, `-h`, `-?`, `/?`) or version token (`--version`, `-V`) as its value. A consumed help token is not live; without a separate version request it is data, not a help request. For a string-valued required option, the command runs and binds that token: on a leaf with required string option `--name`, `myapp group leaf target --name --help` runs with `name="--help"`. A typed required option still validates and converts the consumed value, so it may exit `2` with `Invalid value '--help' ...` instead of running. With a separate live `--version`/`-V` request before `--`, version wins over the consumed token and the command does not run: `myapp group --version leaf target --name --help` prints the version. Pass flag-looking values explicitly with the `=` form (`--name=--help`).

Subcommand-only and grouping nodes keep `--help`/`-h`/`-?`/`/?`/`--version`/`-V`, so a bare help/version token still applies there (`--name /? sub` shows help, like `--name --help sub`). A bare scalar left with no value fails even with its environment variable set — env rescues only omitted valued options, and collections with merged values stay exempt. An empty `=`-form on a non-string option fails as `InvalidValue` (exit `2`); string options still bind empty (and satisfy `Required`; reject emptiness in code when you need non-empty). Other `=`-form value errors stay forgiven beside help. `-help` and `-version` are exact-only full-token unknown options with a did-you-mean pointer, never clusters; neither requests help or version, so both footer hints survive. Global help with no subcommands prints no `<command>` footer.

Help never hides typos. `bogus --help` reports an unknown command (exit 2). On a concrete root with its own run, a miss behind leading help reports `No command found` before forwarding and only a hit forwards to target help. A named grouping parent reports a near miss behind its help token as `Unknown subcommand` with a pointer, while a far miss keeps the subcommand list. During help-target routing, an exact `--version`/`-V` token that co-occurs with the help token before `--` is skipped in either order, including after the leaf name. With the reserved token before or after help, the same near-miss, far-miss, and surplus rows apply (`config --help --version bogus` exits `2` like `config --help bogus`; `config --version sett --help` exits `2` with the same `RequiresSubcommand` text and pointer as `config sett --help`; `config --help --version get` shows `get` help); after the leaf, `config --help get --version extra` exits `2` with the same `Unexpected argument` as `config --help get extra`, and `config --help get --version` shows `get` help. A version-only `config --version bogus` still prints the version (exit `0`). Any bare surplus word after a help-forwarded path fails exit `2` as `Unknown subcommand` (with a pointer when close) else `Unexpected argument`, whether or not the forwarded node owns positionals (example: `config --help get extra` → `2`). When help sits on the leaf itself, a word that fills one of its own positionals is not a surplus (`config get --help bogus` shows `get` help). A reserved version token before the leaf path does not turn that positional into surplus: `config --version get mykey --help` matches `config get mykey --help`, `config --version get extra --help` matches `config get extra --help`, and `config --version set mykey myval --help` matches `config set mykey myval --help`. A version token before the leaf path is skipped by the path-walk prefix, so the leaf binds its positionals normally; an after-leaf version token is skipped by the help-target/surplus scan, so a later bare word remains a surplus. That path-walk skip does not make a dangling leaf option harmless: `config get --format --help` still exits `2`, and a version token before the leaf path keeps the leaf help live (`config --version get --format --help`). `--help=<anything>` fails as `does not accept a value` (`InvalidValue`, exit 2), never help, and `-h=<anything>`, `-?=<anything>` / `/?=<anything>`, `--version=<anything>`, `-V=<anything>` fail the same way with their own token named. `--` ends option matching and blocks help: `-- --help` stays exit 2, and `-- -?` / `-- /?` stay positional.

**Known limitation: non-reserved dash-led tokens.** The transparency guarantee covers exact `--version`/`-V` tokens only. A non-reserved dash-led option still acts as a dash-led breaker when help-target resolution reaches it. Option-interleaved inputs are therefore outside the guarantee; #761 does not claim parity for them.

Value placeholders (`<STRING>`, `<NUMBER>`, `<DATE>`, `<FILE>`, `<DIR>`, `<VALUE>`) live in [Configuration](configuration.md#help-placeholders). Required markers live in [Configuration](configuration.md#required-options-in-help).

## Global Options

Declare an option identically on every command and it becomes one shared global: one value in every scope. A repeated scalar takes the last value. Turn on strict mode with `SetRejectDuplicateOptions(true)` and a repeated scalar valued option fails as `DuplicateOption` (exit 2, `Duplicate option: <display-name>`). Collections stay exempt and accumulate. Flags stay exempt. Values from the environment stay exempt. Flags never consult the environment-variable fallback; env covers omitted valued options only. Option names always match exactly (`Ordinal`). `CaseSensitive` affects `FromAmong` values only; it never affects names. Type a prefix or shorthand and the parser reports `Unknown option` (exit `2`).

## Tokenizer Behavior

The full typing rules live in [Commands](commands.md#how-typing-works). Short version:

- Bare `--verbose` means `true`; bare `--no-verbose` means `false`. `--verbose=<literal>` binds `true`/`false`/`yes`/`no`/`on`/`off`/`1`/`0` case-insensitively; an invalid or empty literal fails as `InvalidValue` (exit `2`). `--verbose <literal>` binds only a valid boolean literal; a non-literal next word stays positional. Negation stays bare-only: `--no-<name>=<anything>` and a boolean word after `--no-<name>` both fail (exit `2`).
- An optional bare valued option never steals a flag-looking word. A trailing bare repeat of a valued scalar fails as missing (exit `2`), even with env set or a prior value. Env covers omitted valued options only. Flags are bare-only and never consult env.
- A required bare valued option takes the next word as its value even when it looks like a flag (`--name --force` binds `--force`). It never takes the `--` separator: `--name --` still fails as missing (exit `2`).
- One splitter parses response files and completion prefixes. It strips `"` and `'` quotes. Any whitespace splits, including newlines. Files have no comments. Response-file rules and limits live in [Commands](commands.md#response-files-file).
