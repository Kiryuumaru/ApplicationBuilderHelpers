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

A root with no command behind it needs a subcommand: running it bare exits `2`. A root with its own run plus subcommands reports a bare non-child first word as `No command found` (`UnknownCommand`, exit `2`), with a pointer only when close. See [Advanced Topics](advanced.md).

Grouping parents without their own `Run` still render a synthetic help page: `myapp deploy --help` lists child commands under `COMMANDS:` and appends `<COMMAND> [ARGS...]` to usage. Leaf-owned options may be interleaved before the child name (`myapp deploy --force prod`) when the current node owns nothing for that spelling and exactly one descendant does: the flag must sit ahead of the child that owns it (a valued option in space form must sit ahead with its value, e.g. `myapp deploy --config v prod-east`), and same-spelling leaves merge only when canonical names match (long name, else short) and shapes agree (flag vs valued, collection, property type) — otherwise the token stays unknown. An unknown option with one unambiguous leaf owner points the error footer at that leaf (`myapp deploy --fast bogus` → `Run 'myapp deploy staging --help' ...`). Short clusters route like long forms (`myapp deploy -f prod` binds `-f` on `deploy prod`). The same single-owner check applies, with a valued short last (`myapp deploy -fc v prod-east`).

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
| `EnvironmentVariable` | Env var used when the flag is omitted (blank counts as omitted) |
| `Required` | Fail with exit `2` when omitted (`""` counts as supplied) |
| `FromAmong` | Only accept these values (enums fill this in automatically) |
| `CaseSensitive` | Match `FromAmong` values with exact case; does NOT affect option NAME matching (names always match exactly) |
| `Secret` | Never print the value; optional-option help always shows `Default: [REDACTED]` to signal secrecy, not that a default exists. The allowed-values list still shows — `[REDACTED]` may be one of the listed values |

Do not declare your own `-h` or `-V`. They belong to `--help` and `--version` and fail the build. `-?` and `/?` are also reserved as bare-token help aliases on every OS; quote them on Unix shells (`'-?'`, `'/?'`) so the shell does not glob them.

Two options in one command must not share a short flag. If a shared base class declares `-l, --log-level` on every command, it becomes one shared global. Leaf commands must then avoid reusing `-l`; use a long-only flag like `--local` instead.

### Env Var Fallback

When you set `EnvironmentVariable` and the user types no flag, the env value fills the option. A typed flag always beats env. Empty or whitespace-only env counts as unset. Env never rescues a flag typed with no value — it covers omitted options only.

### Required Means Present, Not Non-Empty

`Required` checks presence only. Typing `--name ""` or `""` supplies a value, so it passes `Required` and binds as `""`. Only omission fails `Required` with exit `2`. To reject empty text, guard it in code:

```csharp
using ApplicationBuilderHelpers.Exceptions;

if (string.IsNullOrEmpty(Name))
    throw new CommandException("Name must not be empty.", exitCode: 2);
```

Omitted means no value exists (env blank or missing counts as omitted). Empty means a zero-length value was supplied.

### Allowed Values

```csharp
[CommandOption('l', "level", FromAmong = new[] { "debug", "info", "warn", "error" })]
public string Level { get; set; } = "info";
```

Values compare after converting the typed text to your property type. So `02` matches `2` for an `int` option (`--level=02`). Bad values exit `2` with `Must be one of: ...`. See [Custom Type Parsers](custom-type-parsers.md).

### How Typing Works

- Bare flags never eat the next word. `--verbose` means `true`. `--no-verbose` means `false`.
- An option typed with no value never steals a flag-looking word. It fails as missing (exit `2`). A trailing bare repeat of a valued scalar fails the same way, even with env set or a prior value. Env covers omitted options only.
- A flag with `=` never works (exit `2`). Use bare `--verbose` or `--no-verbose`.
- `--no-<name>` works only on `bool` flags and means `false`. `--no-<name>=value` never works.
- Words after the first bare `--` are always positional. `--` itself is swallowed.
- Negative numbers (`-5`) count as positional. Reach a digit short with `-1=value` or after `--`.
- Grouped shorts (`-abc`) expand left to right. A short that needs a value must come last. `-h` and `-V` win mid-group. `-?` and `/?` never expand: `-?` is a bare help token and `-v?` reports `Unknown option: -?`. `-help` and `-version` never expand either (exact tokens only): each reports `Unknown option` with a did-you-mean pointer.
- An empty `=`-form on a non-string option fails as `InvalidValue` (exit `2`); string options still bind empty.
- Unknown options report the name only, never the value: `Unknown option: --pasword`.
- Repeats: flags stay put; repeats of a valued scalar take the last value. Turn on strict mode with `SetRejectDuplicateOptions(true)` and a repeated scalar valued option fails as `DuplicateOption` (exit 2). Collections gather every value and stay exempt, as do flags and environment-supplied values.
- `Required` options show `(required)` in help after the description.
- `@file` expands before parsing. File words splice in place.
- Words after the first bare `--` never expand. They stay literal, even `@@x`.
- `@@x` means literal `@x` before `--`. Lone `@` fails (exit `1`).
- An `@` inside a word never expands. `user@example.com` stays literal.

## Response Files (`@file`)

Put common words in a file. Reference it with `@path`. The library splices file words in place before parsing.

```sh
myapp @args.rsp
myapp deploy @prod.rsp --dry-run
```

`args.rsp` holds plain words:

```text
--verbose --data hello
```

Quote values with spaces. Use `"` or `'`. Newlines count as spaces. Files have no comments. An unterminated quote runs to end of file and is accepted as-is.

```text
--data "hello world"
--verbose
```

Rules:

- Expansion runs before completion, help, and parsing. Help from a file renders help (exit `0`). An expansion fault still fails (exit `1`).
- A `@file` token expands wherever it appears before `--`. After `--` it stays literal.
- `@@x` means literal `@x`. This works on the command line and inside files.
- Lone `@` names no file. It fails (exit `1`).
- Relative paths resolve against the current directory. Absolute paths work from any directory.
- Files can reference files. Reuse of one file twice succeeds. A cycle fails.
- Completion on a partial `@word` returns nothing (exit `0`). Other probes expand first, then complete.

Limits:

| Limit | Value |
|---|---|
| Max file size | 1 MB (1048576 bytes) per file |
| Max total read | 4 MB (4194304 bytes) |
| Max expanded args | 10000 args |
| Max nesting depth | 8 levels |

Errors exit `1` as `Fault` with a help-only footer. The message names the cause:

| Cause | Message contains |
|---|---|
| Lone `@` | `names no file` |
| Missing file | `not found` |
| Unreadable path | `is unreadable` |
| File over 1 MB | `exceeds size limit` |
| Total over 4 MB | `total size limit` |
| Over 10000 args | `exceeds 10000 arguments` |
| Over depth 8 | `exceeds nesting depth 8` |
| Cycle | `forms a cycle` |

The 10000-arg limit applies to the final expanded argv, including words typed on the command line. A bare `--` on the command line stops expansion after it; a `--` inside a file is an ordinary word and does not stop expansion.

This is argv only. It never reads app settings. For settings reuse, see `@ref:` in [Configuration](configuration.md).

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
| `Required` | Fail with exit `2` when omitted (`""` counts as supplied) |
| `FromAmong` | Only accept these values |
| `CaseSensitive` | Match values with exact case; does NOT affect argument NAME matching |
| `Secret` | Never print the value |

Typing `""` counts as supplied and binds as `""` for text. It satisfies `Required`; only omission fails `Required`. Check with `string.IsNullOrEmpty`, not `== null`. Need non-empty text? Guard it in code (see Options above).

Arguments belong to one command only. A root positional stays hidden from subcommands. It binds a bare word only when the root has no children; with children the miss check runs first and a bare non-child word fails as `No command found` (exit `2`). A surplus word on a leaf fails with `Unexpected argument` (exit `2`).

## Get Services in a Command

Define these markers once in your app. The library ships no service attributes, so bare `[FromServices]` does not compile alone. The gate matches by simple name (`FromServicesAttribute` / `FromKeyedServicesAttribute`) in any namespace:

```csharp
using System;

namespace MyApp;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FromServicesAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class FromKeyedServicesAttribute(object key) : Attribute
{
    public object Key { get; } = key;
}
```

Do not use the built-in `Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute` on properties. It targets parameters only and never compiles there. Use the shim above instead.

Mark a property with your `[FromServices]` shim and the library fills it before `Run`:

```csharp
using MyApp;

public class BuildCommand : Command
{
    [FromServices]
    public IMyService Service { get; set; } = null!;

    [FromKeyedServices("primary")]
    public ICache Cache { get; set; } = null!;

    protected override ValueTask Run(
        ApplicationHost<HostApplicationBuilder> applicationHost,
        CancellationToken cancellationToken)
    {
        // Service and Cache are already filled from the per-run scope.
        return ValueTask.CompletedTask;
    }
}
```

Pass the key as the constructor argument. The key is an `object` and the `Key` property carries it. The gate also reads a `Key = ...` named argument from shims shaped that way.

Rules:

- Keep input and services apart. A property with both `[CommandOption]` and `[FromServices]` always fails the build (exit `1`).
- Hiding with `new` never excuses a clash. Both copies are checked.
- Services resolve per run. Help, version, and validation paths create no scope.
- A missing service fails with exit `1`, never exit `2`.

## Run Cleanup Code

Ask for `LifetimeService` in your command with your `[FromServices]` shim (`using MyApp;`), then register callbacks:

```csharp
using MyApp;

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

Duplicate errors list first, then missing, then invalid-value errors. An explicit bare valued option fails as missing even with env set. Env rescues only omitted options. Error footers pair a route-relative `--help` hint with a global `--version` hint. A concrete root with its own run reports a leading bare non-child word as `UnknownCommand` before the `RequiresSubcommand` guard; a miss behind leading help tokens reports `No command found` before forwarding and only hits forward to target help. A named grouping parent reports a near miss behind its help token as `Unknown subcommand` with a pointer, while a far miss keeps the subcommand list. Full table lives in [API Reference](api-reference.md). Full help rules live in [Advanced Topics](advanced.md).

Bad shell names and install errors exit `2` on stderr. File errors exit `1` on stderr. Bare `completions` falls through to normal parsing.

## Exit Codes

`0` means success, `2` means bad input, `1` means failure, `130` means canceled. Success answers use stdout. Errors use stderr. Full table lives in [API Reference](api-reference.md). Commands run the 8 shared setup steps in [Application Dependencies](application-dependencies.md).
