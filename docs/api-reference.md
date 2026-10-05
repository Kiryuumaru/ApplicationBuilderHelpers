# API Reference

Look up every class and method you call to build your own app. Start with `ApplicationBuilder`, then define commands, then share setup.

New here? Build your first app in [Getting Started](getting-started.md). This page assumes you already ran `Hello, Alice!` once.

| Task | Go to |
|---|---|
| Build and run your app | [Build and Run](#build-and-run-your-app) |
| Define commands, options, arguments | [Define Commands](#define-commands) |
| Share services and settings | [Share Setup](#share-setup) |
| Support your own option types | [Read Option Types](#read-option-types) |
| Style help, reuse settings | [Style Help and Reuse Settings](#style-help-and-reuse-settings) |
| Fail with the right exit code | [Handle Errors and Exit Codes](#handle-errors-and-exit-codes) |

Typing rules live in [Commands](commands.md). Setup order lives in [Application Dependencies](application-dependencies.md). Help placeholders and widths live in [Configuration & Themes](configuration.md). Custom parser examples live in [Custom Type Parsers](custom-type-parsers.md). Subcommands, web hosts, and completion live in [Advanced Topics](advanced.md).

## Build and Run Your App

Create a builder, register commands, then run it:

```csharp
// Program.cs
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;

return await ApplicationBuilder.Create()
    .SetExecutableName("myapp")
    .AddCommand<HelloCommand>()
    .RunAsync(args);
```

`Create()` returns an empty builder with 24 parsers ready. `RunAsync(args)` parses the arguments, runs the matched command, and returns the exit code.

| Method | Use it to |
|---|---|
| `Create()` | Create an empty builder with built-in parsers ready |
| `AddCommand<TCommand>()` | Register a command type; each run gets a fresh instance |
| `AddCommand(command)` | Register one shared instance; state carries between runs |
| `AddApplication<TModule>()` | Register a shared setup module (needs no constructor values) |
| `AddApplication(module)` | Register a setup module that needs constructor values |
| `AddCommandTypeParser<TParser>()` | Register a custom option-type parser for every command |
| `AddCommandTypeParser(parser)` | Register a parser instance you already built |
| `SetTheme<TTheme>()` | Pick a help color theme by type |
| `SetTheme(theme)` | Apply a theme instance you already built |
| `SetExecutableName(name)` | Set the name shown in help and error hints |
| `SetExecutableTitle(title)` | Set the title shown in help headers |
| `SetExecutableDescription(description)` | Set the description shown in help |
| `SetExecutableVersion(version)` | Set the version printed by `--version` |
| `SetHelpWidth(width)` | Set help width; needs a positive number |
| `SetHelpBorderWidth(width)` | Set help padding; `0` removes it |
| `RunAsync(args, cancellationToken)` | Parse and run; returns the exit code |

All four `SetExecutable*` setters are optional. When you skip them, the library reads your entry assembly instead. Passing `null` throws. `RunAsync` throws when `args` is `null`.

Help renders at 120 columns when you skip `SetHelpWidth`. Values below 60 render at 60.

## Define Commands

Extend `Command` for console apps. Override `Run` with your logic. Return normally for success (exit `0`).

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

[Command("greet", description: "Greet by name")]
public class GreetCommand : Command
{
    [CommandOption('n', "name", Description = "Who to greet")]
    public string Name { get; set; } = "World";

    protected override ValueTask Run(
        ApplicationHost<HostApplicationBuilder> applicationHost,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}
```

Need a server or a custom host? Extend `Command<THostBuilder>` instead, build the host in `ApplicationBuilder(stoppingToken)`, and read it back through `applicationHost.Builder` in `Run`. See [Advanced Topics](advanced.md).

Ignore the obsolete `Run` overload that takes a `CancellationTokenSource`. Override the `CancellationToken` overload above. Do not implement `ICommand` directly; derive from `Command<T>` instead.

### `[Command]`

Mark a command class with its route name and help text:

```csharp
[Command(description: "Runs at the root, no name")]
[Command("greet", description: "Greet by name")]
[Command("deploy prod", description: "Deploy to production")]
```

Use spaces for subcommands. See [Commands](commands.md) for naming rules.

### `[CommandOption]`

Mark a property as a named flag. Pick one of three forms:

```csharp
[CommandOption('v', "verbose", Description = "Enable verbose output")]
[CommandOption('v')]            // Short only: -v
[CommandOption("verbose")]      // Long only: --verbose
```

A single-letter long name also answers its single-dash alias: `[CommandOption("a")]` binds both `-a` and `--a`. An explicit short wins.

| Setting | What it does |
|---|---|
| `Description` | Help text |
| `EnvironmentVariable` | Env var used when the flag is not typed |
| `Required` | Fail with exit `2` when not supplied |
| `FromAmong` | Only accept these values |
| `CaseSensitive` | Match `FromAmong` with exact case; does NOT affect option NAME matching (names always match exactly) |
| `Secret` | Never print the value; help shows `[REDACTED]` |

A typed flag always beats the env fallback. Env covers omitted options only. Do not declare your own `-h` or `-V`; they belong to help and version. See [Commands](commands.md) for typing rules.

### `[CommandArgument]`

Mark a property as a positional word:

```csharp
[CommandArgument(Name = "source", Position = 0, Description = "Source file", Required = true)]
public string SourceFile { get; set; } = "";
```

| Setting | What it does |
|---|---|
| `Name` | Display name in help; defaults to the property name |
| `Position` | Which positional word, starting at `0` |
| `Description` | Help text |
| `Required` | Fail with exit `2` when not supplied |
| `FromAmong` | Only accept these values |
| `CaseSensitive` | Match values with exact case; does NOT affect argument NAME matching |
| `Secret` | Never print the value |

Typing `""` counts as supplied. Check text with `string.IsNullOrEmpty`, not `== null`.

### Get Services in `Run`

Define the `FromServices` / `FromKeyedServices` shims once in your app (see [Commands](commands.md)). Mark a property with your `[FromServices]` shim and the library fills it from the per-run scope before `Run`. A missing service fails with exit `1`. Never mix `[CommandOption]` and `[FromServices]` on one property; the build fails with exit `1`.

Ask for `LifetimeService` with your `[FromServices]` shim to register shutdown callbacks (`ApplicationExitingCallback`, `ApplicationExitedCallback`). See [Commands](commands.md).

## Share Setup

Extend `ApplicationDependency` to group services, settings, and startup steps once. Register it with `AddApplication`. Every command then gets the same setup.

```csharp
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

public class CoreApplication : ApplicationDependency
{
    public override void AddServices(
        ApplicationHostBuilder applicationBuilder,
        IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
    }
}

// Program.cs
return await ApplicationBuilder.Create()
    .AddApplication<CoreApplication>()
    .AddCommand<MyCommand>()
    .RunAsync(args);
```

Override only the steps your module owns. Each step runs on every module before the next step starts.

| Order | Override | Use it to |
|---|---|---|
| 1 | `CommandPreparation` | Register custom type parsers before parsing |
| 2 | `BuilderPreparation` | Adjust the host builder before settings load |
| 3 | `AddConfigurations` | Add settings sources |
| 4 | `AddServices` | Register services |
| 5 | `AddMiddlewares` | Wire middleware onto the built app |
| 6 | `AddMappings` | Map endpoints (web apps) |
| 7 | `RunPreparation` | Quick sync setup before the run |
| 8 | `RunPreparationAsync` | Async setup before the run (all modules run at once) |

The command's own hooks run as part of the same pipeline. Full order and replacement guidance live in [Application Dependencies](application-dependencies.md).

During setup your module receives an `ApplicationHostBuilder` with `Builder` (the underlying host builder), `Services` (the service collection), and `Configuration` (the app settings). During `Run` your command receives an `ApplicationHost` with `Host` (the built host), `Services` (the service provider), and `Builder` (the original host builder).

## Read Option Types

The library ships 24 parsers: `AbsolutePath`, `bool`, `byte`, `char`, `DateOnly`, `DateTime`, `DateTimeOffset`, `decimal`, `double`, `FileInfo`, `float`, `Guid`, `int`, `long`, `sbyte`, `short`, `string`, `TimeOnly`, `TimeSpan`, `uint`, `ulong`, `Uri`, `ushort`, `Version`.

Add a custom parser only for your own domain types. The easy way covers most cases: extend `CommandTypeParser<T>`, override `ParseValue`, and register it once.

```csharp
// Program.cs
using ApplicationBuilderHelpers;

ApplicationBuilder.Create()
    .AddCommandTypeParser<CurrencyTypeParser>()
    .AddCommand<InvoiceCommand>()
    .RunAsync(args);
```

On failure, return `null` and set `validateError` to the reason. That reason appears in the `InvalidValue` error (exit `2`). Keep `Parse` pure: no side effects. Parse numbers and dates with `CultureInfo.InvariantCulture`. Full examples live in [Custom Type Parsers](custom-type-parsers.md).

Need control over defaults and collection storage? Implement `ICommandTypeParser` directly:

| Member | Implement it to |
|---|---|
| `Type` | Name the CLR type this parser converts |
| `Parse` | Convert CLI text; return `null` plus a reason on failure |
| `GetString` | Format a value for help defaults and completion |
| `GetDefaultValue` | Supply the value used when input is omitted |
| `CreateTypedArray` | Build the `T[]` used for repeatable options |
| `CreateTypedList` | Build the `List<T>` used for repeatable options |

Repeat an option to fill `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, or `IList<T>`. Registering a parser for an enum type turns off automatic enum-value help for that enum. Registering a parser for a built-in type replaces the built-in one.

## Style Help and Reuse Settings

### Console Themes

The library ships 6 themes. `DefaultConsoleTheme` applies unless you pick another. Piped or redirected output renders plain; themes apply to interactive terminals only.

| Theme | Pick it when |
|---|---|
| `DefaultConsoleTheme` | You want the standard colors (start here) |
| `MonochromeConsoleTheme` | The terminal has no color |
| `HighContrastConsoleTheme` | You need maximum contrast |
| `MinimalConsoleTheme` | You want help to stay quiet beside output |
| `DarkConsoleTheme` | The terminal uses a dark background |
| `LightConsoleTheme` | The terminal uses a light background |

```csharp
// Program.cs
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Themes;

// Use a shared instance
ApplicationBuilder.Create()
    .SetTheme(DarkConsoleTheme.Instance);

// Or pick a theme by type
ApplicationBuilder.Create()
    .SetTheme<DarkConsoleTheme>();
```

Write your own by implementing `IConsoleTheme` with 6 colors (`HeaderColor`, `FlagColor`, `ParameterColor`, `DescriptionColor`, `SecondaryColor`, `RequiredColor`) and passing an instance to `SetTheme`. Placeholder shapes and required markers live in [Configuration & Themes](configuration.md).

### Reuse One Setting Inside Another (`@ref:`)

Point one setting at another key instead of copying the value. Chains can nest (`A` points at `B`, `B` holds the value). Matching ignores case. Chains resolve at most 32 hops. A cycle or missing key fails to resolve instead of looping.

| Method | Use it to |
|---|---|
| `GetRefValue("Key")` | Return the final value; throws `NoConfigValueException` when nothing resolves |
| `TryGetRefValue("Key", out var resolved)` | Return `true` and set `resolved` on success |
| `ContainsRefValue("Key")` | Check whether the key resolves |
| `GetRefValueOrDefault("Key", "fallback")` | Return the resolved value, or `"fallback"` instead |

```csharp
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;

string connStr = configuration.GetRefValue("ConnectionString");
```

`NoConfigValueException` names the missing key: `{name} config is empty`.

### Exit Contract

| Outcome | Exit code | Stream |
|---|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` | stdout |
| Completion candidates and shims | `0` | stdout |
| Usage error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` only with `SetRejectDuplicateOptions(true)` — repeats otherwise take the last value) | `2` | stderr |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` | stderr |
| Cancellation (`CancellationToken` / Ctrl+C / SIGTERM) | `130` | none (shutdown diagnostics use stderr) |
| Host lifetime diagnostics | — | stderr or suppressed, never stdout |

Duplicate errors list first, then missing, then invalid-value errors. Missing-only keeps kind `MissingRequired`, duplicate-only keeps kind `DuplicateOption`, any invalid line makes the kind `InvalidValue`. Every help screen lists `-V, --version` under `GLOBAL OPTIONS:`. Usage-error footers pair a route-relative `--help` hint with a global `--version` hint, except when the failing call already contained `--help`/`-h`, when only the `--version` hint survives. `Fault`/`NoImplementation` keep the `--help`-only footer. Response-file expansion faults exit `1` as `Fault` with the `--help`-only footer. Expansion runs before completion, help, and parsing, so a fault beats help. Full rules and limits live in [Commands](commands.md#response-files-file).

```csharp
public enum CommandErrorKind
{
    Fault,
    UnknownOption,
    MissingRequired,
    RequiresSubcommand,
    InvalidValue,
    UnknownCommand,
    DuplicateOption, // Strict `SetRejectDuplicateOptions(true)` only; repeats otherwise take the last value.
    NoImplementation,
}

public class CommandException : Exception
{
    public int ExitCode { get; }
    public CommandErrorKind Kind { get; }
    public string? CommandName { get; }
    public CommandException(int exitCode);
    public CommandException(string message, int exitCode);
    public CommandException(string message, int exitCode, CommandErrorKind kind, string? commandName = null);
    public CommandException(int exitCode, CommandErrorKind kind);
}
```

`NoConfigValueException` names the missing key: `{name} config is empty`.

## Handle Errors and Exit Codes

`RunAsync` returns the exit code. Return normally from `Run` for success.

| Code | Meaning | Stream |
|---|---|---|
| `0` | Success; also `--help`, `--version`, and completion answers | stdout |
| `2` | Bad input: unknown option or command, missing required value, bad value, missing subcommand | stderr |
| `1` or custom | Your `CommandException` exit code; unknown failures exit `1` | stderr |
| `130` | Canceled (Ctrl+C, SIGTERM, or canceled token) | none (shutdown diagnostics use stderr) |

Shell completion answers before help and parsing. One pre-validation gate forgives help and version: either skips all validation when its flag is present, help beats version, and unknown/misuse/requires-subcommand/invalid-literal errors still beat both. An invalid `FromAmong` value beats both the same way, as does a dangling valued option beside help (exit `2`). Usage errors print a footer pointing at the right `--help`. Full help and precedence rules live in [Advanced Topics](advanced.md).

Throw `CommandException` from `Run` to return a custom exit:

```csharp
using ApplicationBuilderHelpers.Exceptions;

throw new CommandException("Configuration missing", exitCode: 3);
```

`CommandException` carries `ExitCode`, `Kind` (usage vs. fault, which picks the footer), and optional `CommandName` (which scopes the help hint; the version hint stays global). Constructors cover code-only, message plus code, message plus code plus kind, and code plus kind. Keep messages and help text secret-free; never put values or stack traces in them.
