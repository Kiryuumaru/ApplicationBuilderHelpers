# Commands

Commands are classes. Options are named flags. Arguments are positional words. Subcommands are space-separated names.

## Define a Command

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

[Command("build", description: "Build the project")]
public class BuildCommand : Command
{
    protected override ValueTask Run(
        ApplicationHost<HostApplicationBuilder> applicationHost,
        CancellationToken cancellationToken)
    {
        // Return normally for success (exit 0).
        return ValueTask.CompletedTask;
    }
}
```

Extend `Command`. Override `Run`. Mark the class with `[Command]`.

### Naming Commands

```csharp
[Command(description: "Description only")]          // Runs at the root, no name
[Command("name")]                                     // Named command
[Command("name", description: "With description")]    // Named with help text
```

Use spaces for subcommands:

```csharp
[Command("deploy prod", description: "Deploy to production")]
```

Rules for names:

| Name | Result |
|---|---|
| No name (description only) | Runs at the root |
| `""` or only spaces | Build error, exit `1`: name must not be empty |
| Name starting with `-` | Build error, exit `1`: names must not start with `-` |
| Extra spaces (`"a  b"`) | Treated as single spaces |

A root with no command behind it needs a subcommand: running it bare exits `2`. See [Advanced Topics](advanced.md).

### Command Base Classes

| Base class | Use for |
|---|---|
| `Command` | Console apps and workers (default) |
| `Command<THostApplicationBuilder>` | Web apps or custom hosts |

## Options

Add a property with `[CommandOption]`:

```csharp
[CommandOption('v', "verbose", Description = "Enable verbose output")]
public bool Verbose { get; set; }

[CommandOption('c', "config", Description = "Config file path", EnvironmentVariable = "APP_CONFIG")]
public string? ConfigPath { get; set; }

[CommandOption("timeout", Description = "Timeout in seconds")]
public int Timeout { get; set; } = 30;
```

Three forms:

```csharp
[CommandOption('s', "long-name")]   // Short and long: -s, --long-name
[CommandOption('s')]                 // Short only: -s
[CommandOption("long-name")]         // Long only: --long-name
```

A single-letter long name also answers its single-dash alias: `[CommandOption("a")]` binds both `-a` and `--a`. An explicit short wins.

### Option Settings

| Setting | What it does |
|---|---|
| `Description` | Help text |
| `EnvironmentVariable` | Env var used when the flag is not typed |
| `Required` | Fail with exit `2` when not supplied |
| `FromAmong` | Only accept these values (enums fill this in automatically) |
| `CaseSensitive` | Match `FromAmong` values with exact case; does NOT affect option NAME matching (names always match exactly) |
| `Secret` | Never print the value; help shows `[REDACTED]` |

Do not declare your own `-h` or `-V`. They belong to `--help` and `--version` and fail the build. `-?` and `/?` are also reserved as bare-token help aliases on every OS; quote them on Unix shells (`'-?'`, `'/?'`) so the shell does not glob them.

Two options in one command must not share a short flag. If a shared base class declares `-l, --log-level` on every command, it becomes one shared global. Leaf commands must then avoid reusing `-l`; use a long-only flag like `--local` instead.

### Env Var Fallback

When you set `EnvironmentVariable` and the user types no flag, the env value fills the option. A typed flag always beats env. Empty env counts as unset. Env never rescues a flag typed with no value — it covers omitted options only.

### Allowed Values

```csharp
[CommandOption('l', "level", FromAmong = new[] { "debug", "info", "warn", "error" })]
public string Level { get; set; } = "info";
```

Values compare after converting the typed text to your property type. So `02` matches `2` for an `int` option (`--level=02`). Bad values exit `2` with `Must be one of: ...`. See [Custom Type Parsers](custom-type-parsers.md).

### How Typing Works

- Bare flags never eat the next word. `--verbose` means `true`. Use `--verbose=off` for a value.
- An option typed with no value never steals a flag-looking word. It fails as missing (exit `2`). A trailing bare repeat of a valued scalar fails the same way, even with env set or a prior value. Env covers omitted options only.
- `=`-form flags accept `true/false/yes/no/on/off/1/0` in any case. Anything else exits `2`.
- `--no-<name>` works only on `bool` flags and means `false`. `--no-<name>=value` never works.
- Words after the first bare `--` are always positional. `--` itself is swallowed.
- Negative numbers (`-5`) count as positional. Reach a digit short with `-1=value` or after `--`.
- Grouped shorts (`-abc`) expand left to right. A short that needs a value must come last. `-h` and `-V` win mid-group. `-?` and `/?` never expand: `-?` is a bare help token and `-v?` reports `Unknown option: -?`.
- Unknown options report the name only, never the value: `Unknown option: --pasword`.
- Repeats: flags stay put; repeats of a valued scalar take the last value. Turn on strict mode with `SetRejectDuplicateOptions(true)` and a repeated scalar valued option fails as `DuplicateOption` (exit 2). Collections gather every value and stay exempt, as do flags and environment-supplied values.
- `Required` options show `(required)` in help after the description.

## Arguments

Add a property with `[CommandArgument]`:

```csharp
[CommandArgument(Name = "source", Position = 0, Description = "Source file", Required = true)]
public string SourceFile { get; set; } = "";

[CommandArgument(Name = "dest", Position = 1, Description = "Destination")]
public string? DestPath { get; set; }
```

| Setting | What it does |
|---|---|
| `Name` | Display name in help |
| `Position` | Which positional word (starts at `0`) |
| `Description` | Help text |
| `Required` | Fail with exit `2` when not supplied |
| `FromAmong` | Only accept these values |
| `CaseSensitive` | Match values with exact case; does NOT affect argument NAME matching |
| `Secret` | Never print the value |

Typing `""` counts as supplied and binds as `""` for text. Check with `string.IsNullOrEmpty`, not `== null`.

Arguments belong to one command only. A root positional stays hidden from subcommands, so a surplus word fails with `Unexpected argument` (exit `2`).

## Get Services in a Command

Mark a property with `[FromServices]` and the library fills it before `Run`:

```csharp
public class BuildCommand : Command
{
    [FromServices]
    public IMyService Service { get; set; } = null!;

    protected override ValueTask Run(
        ApplicationHost<HostApplicationBuilder> applicationHost,
        CancellationToken cancellationToken)
    {
        // Service is already filled from the per-run scope.
        return ValueTask.CompletedTask;
    }
}
```

For a keyed service, define a property-capable attribute with the same name:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FromKeyedServicesAttribute(object key) : Attribute
{
    public object Key { get; } = key;
}
```

Rules:

- Keep input and services apart. A property with both `[CommandOption]` and `[FromServices]` always fails the build (exit `1`).
- Hiding with `new` never excuses a clash. Both copies are checked.
- Services resolve per run. Help, version, and validation paths create no scope.
- A missing service fails with exit `1`, never exit `2`.

## Run Cleanup Code

Ask for `LifetimeService` in your command, then register callbacks:

```csharp
[FromServices]
public LifetimeService Lifetime { get; set; } = null!;

// When the app starts stopping:
Lifetime.ApplicationExitingCallback(() => Console.WriteLine("stopping"));
// After shutdown completes:
Lifetime.ApplicationExitedCallback(() => Console.WriteLine("stopped"));
```

`Exiting` runs when the command or host stops. `Exited` runs after shutdown. Each phase runs once even when shutdown is signaled twice. On failure only `Exited` runs.

## Register Commands

```csharp
// Fresh instance every run (usual case):
ApplicationBuilder.Create().AddCommand<MyCommand>();

// One shared instance (state carries between runs):
ApplicationBuilder.Create().AddCommand(new MyCommand());
```

Commands added between runs appear on the next run.

## Shell Completion

The library answers shell TAB probes before help and parsing. A command named `complete` never runs.

| Typed words | What happens |
|---|---|
| `complete --position N "<line>"` | Prints one candidate per line to stdout, exits `0`. Bare `complete` lists subcommands. |
| `completions script <bash\|zsh\|pwsh\|powershell\|fish>` | Prints a TAB shim for that shell. |
| `completions install [--shell <...>] [--dry-run]` | Writes the shim into your shell file. `--dry-run` prints `would-write: <path>` and changes nothing. |
| `completions uninstall [--shell <...>]` | Removes the shim. |

### Exit Contract

| Outcome | Exit code | Stream |
|---|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` | stdout |
| Completion candidates and shims | `0` | stdout |
| Usage error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` only with `SetRejectDuplicateOptions(true)` — repeats otherwise take the last value) | `2` | stderr |
| Unexpected fault (`Fault`, `NoImplementation`) or `Run` throwing `CommandException` | `1`, or `ex.ExitCode` | stderr |
| External cancellation (outer `CancellationToken` / Ctrl+C / SIGTERM) | `130` | none (shutdown diagnostics use stderr) |
| Host lifetime diagnostics | — | stderr or suppressed, never stdout |

Host lifetime messages never reach stdout. They write to stderr or stay silent.

Duplicate errors list first, then missing, then invalid-value errors. An explicit bare valued option fails as missing even with env set. Env rescues only omitted options. Error footers pair a route-relative `--help` hint with a global `--version` hint. Full table lives in [API Reference](api-reference.md). Full help rules live in [Advanced Topics](advanced.md).

Bad shell names and install errors exit `2` on stderr. File errors exit `1` on stderr. Bare `completions` falls through to normal parsing.

## Exit Codes

`0` means success, `2` means bad input, `1` means failure, `130` means canceled. Success answers use stdout. Errors use stderr. Full table lives in [API Reference](api-reference.md). Commands run the 8 shared setup steps in [Application Dependencies](application-dependencies.md).
