# API Reference

## ApplicationBuilder

The entry point fluent builder.

```csharp
public class ApplicationBuilder : ICommandBuilder
```

### Static

| Method | Returns | Description |
|---|---|---|
| `Create()` | `ApplicationBuilder` | Creates a new builder with all built-in type parsers |

### Instance Methods

| Method | Returns | Description |
|---|---|---|
| `AddCommand<TCommand>()` | `ApplicationBuilder` | Register a command type (fresh instance per `RunAsync` run) |
| `AddCommand(ICommand)` | `ApplicationBuilder` | Register a command instance (same reference reused across runs) |
| `AddApplication<T>()` | `ApplicationBuilder` | Register an application dependency by type |
| `AddApplication(IApplicationDependency)` | `ApplicationBuilder` | Register an application dependency instance |
| `AddCommandTypeParser<T>()` | `ApplicationBuilder` | Register a custom type parser |
| `SetTheme<TTheme>()` | `ApplicationBuilder` | Set theme by type |
| `SetTheme(IConsoleTheme)` | `ApplicationBuilder` | Set theme by instance |
| `SetExecutableName(string)` | `ApplicationBuilder` | Override auto-detected name |
| `SetExecutableTitle(string)` | `ApplicationBuilder` | Override auto-detected title |
| `SetExecutableDescription(string)` | `ApplicationBuilder` | Override auto-detected description |
| `SetExecutableVersion(string)` | `ApplicationBuilder` | Override auto-detected version |
| `SetHelpWidth(int)` | `ApplicationBuilder` | Set help line width (must be positive; `0`/negatives throw; default `120` when unset; effective width floored at `60` = `20` left + `40` right; `80` is a common console-width convention) |
| `SetHelpBorderWidth(int)` | `ApplicationBuilder` | Set help border indentation |
| `RunAsync(string[], CancellationToken)` | `Task<int>` | Parse args and run (`0` success/help/version, `2` usage, `1`-or-custom fault, `130` cancel — see `CommandException` below) |

Repeated `RunAsync` calls rebuild the command topology from live registrations, so late `AddCommand` / `AddCommandTypeParser` calls are visible on the next run; type-registered commands get a fresh instance per run while instance registrations reuse the same reference. Per-`Type` reflection descriptors are cached per builder (immutable snapshots, double-checked lock, miss-counted by `CommandReflectionCache.BuildCount`) and reassembled into fresh per-run nodes, with enum `FromAmong` auto-population (options and positional arguments) suppressed when a live parser exists for that enum type. The per-`Type` service-injection plan (property plus optional keyed-service key, CLI-bound set hoisted in) is cached separately via its own `TypePlanCache` instance sharing the same double-checked-lock core; CLI-bound identity is one canonical predicate (`CommandReflectionCache.IsCliBound`, next to `Walk`), and any dual-marked property in the walk chain always throws `InvalidOperationException` (fault, exit 1). The cache layer is thread-safe via immutable descriptors with locked population plus per-run reassembly, but `ApplicationBuilder` collections, shared console output, instance-registered commands, and user command state remain caller-responsibility and are not safe for concurrent runs/mutation.

## Command

```csharp
public abstract class Command : Command<HostApplicationBuilder>
public abstract class Command<THostApplicationBuilder> : ApplicationDependency, ICommand
```

### Abstract Members (to override)

| Member | Returns | Description |
|---|---|---|
| `Run(ApplicationHost<THostApplicationBuilder>, CancellationToken)` | `ValueTask` | Command logic (return normally on success; throw `CommandException` for errors; cancellation maps to 130) |
| `ApplicationBuilder(CancellationToken)` | `ValueTask<THostApplicationBuilder>` | Create host builder (only on generic variant) |

### Inherited from ApplicationDependency

All lifecycle methods are available — see `ApplicationDependency` below.

## ApplicationDependency

```csharp
public abstract class ApplicationDependency : IApplicationDependency
```

### Virtual Lifecycle Methods (all optional to override)

| Method | Signature |
|---|---|
| `CommandPreparation` | `(ApplicationBuilder applicationBuilder)` |
| `BuilderPreparation` | `(ApplicationHostBuilder applicationBuilder)` |
| `AddConfigurations` | `(ApplicationHostBuilder appBuilder, IConfiguration config)` |
| `AddServices` | `(ApplicationHostBuilder appBuilder, IServiceCollection services)` |
| `AddMiddlewares` | `(ApplicationHost appHost, IHost host)` |
| `AddMappings` | `(ApplicationHost appHost, IHost host)` |
| `RunPreparation` | `(ApplicationHost appHost)` |
| `RunPreparationAsync` | `(ApplicationHost appHost, CancellationToken ct)` |

## ApplicationHost

```csharp
public abstract class ApplicationHost
public class ApplicationHost<THostApplicationBuilder> : ApplicationHost
```

### Properties

| Property | Type | Description |
|---|---|---|
| `Host` | `IHost` | The built host |
| `Services` | `IServiceProvider` | Service provider |
| `Builder` | `IHostApplicationBuilder` / `THostApplicationBuilder` | The host builder |

## ApplicationHostBuilder

```csharp
public abstract class ApplicationHostBuilder
public class ApplicationHostBuilder<THostApplicationBuilder> : ApplicationHostBuilder
```

### Properties

| Property | Type | Description |
|---|---|---|
| `Builder` | `IHostApplicationBuilder` / `THostApplicationBuilder` | The host builder |
| `Services` | `IServiceCollection` | Service collection |
| `Configuration` | `IConfiguration` | Configuration |

## Interfaces

### ICommand

```csharp
public interface ICommand : IApplicationDependency
```

### IApplicationDependency

```csharp
public interface IApplicationDependency
{
    void CommandPreparation(ApplicationBuilder applicationBuilder);
    void BuilderPreparation(ApplicationHostBuilder applicationBuilder);
    void AddConfigurations(ApplicationHostBuilder, IConfiguration);
    void AddServices(ApplicationHostBuilder, IServiceCollection);
    void AddMiddlewares(ApplicationHost, IHost);
    void AddMappings(ApplicationHost, IHost);
    void RunPreparation(ApplicationHost);
    ValueTask RunPreparationAsync(ApplicationHost, CancellationToken);
}
```

### ICommandTypeParser

```csharp
public interface ICommandTypeParser
{
    Type Type { get; }
    object? Parse(string? value, out string? validateError);
    string? GetString(object? value);
    object? GetDefaultValue();
    Array CreateTypedArray(int length);
    IList CreateTypedList(int capacity);
}
```

### IConsoleTheme

```csharp
public interface IConsoleTheme
{
    ConsoleColor HeaderColor { get; }
    ConsoleColor FlagColor { get; }
    ConsoleColor ParameterColor { get; }
    ConsoleColor DescriptionColor { get; }
    ConsoleColor SecondaryColor { get; }
    ConsoleColor RequiredColor { get; }
}
```

### ICommandBuilder

```csharp
public interface ICommandBuilder : ICommandTypeParserCollection, IApplicationDependencyCollection
```

## Attributes

### CommandAttribute

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    public CommandAttribute(string? description = null);
    public CommandAttribute(string name, string? description = null);
    public string? Term { get; set; }
    public string? Description { get; set; }
}
```

### `CommandAttribute.Term` Validation Contract

Build-time guard — violations throw `InvalidOperationException` (fault, exit `1`): null `Term` merges at the root; non-null empty/whitespace throws (`term must not be empty or whitespace`); any dash-led part throws (`command names must not start with '-'`); multi-space normalizes via `Split(' ', RemoveEmptyEntries)`. Enforced at both `src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:140-159` (`FromCommand`) and `src/ApplicationBuilderHelpers/CommandLineParser/CommandHierarchyBuilder.cs:82-88,193-204` (hierarchy build + abstract-base match). See [Commands](commands.md#term-validation-contract) and [Advanced Topics](advanced.md#bare-root-and-help-first).

### CommandOptionAttribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class CommandOptionAttribute : Attribute
{
    public CommandOptionAttribute(char shortTerm, string term);
    public CommandOptionAttribute(char shortTerm);
    public CommandOptionAttribute(string term);
    public string? Term { get; set; }
    public char? ShortTerm { get; set; }
    public string? EnvironmentVariable { get; set; }
    public bool Required { get; set; }
    public string? Description { get; set; }
    public object[] FromAmong { get; set; }
    public bool CaseSensitive { get; set; }
    public bool Secret { get; set; }
}
```

`ShortTerm` values `'h'` / `'V'` are reserved for help/version — declaring either throws `InvalidOperationException` at registration (fail-closed, `CommandHierarchyBuilder.cs:471-497`), unless `LongName` is `help` for `-h`; `-V` always throws (no version node, gateway-only); use the long `Term` form instead.

### CommandArgumentAttribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class CommandArgumentAttribute : Attribute
{
    public CommandArgumentAttribute();
    public CommandArgumentAttribute(string name);
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Position { get; set; }
    public bool Required { get; set; }
    public object[] FromAmong { get; set; }
    public bool CaseSensitive { get; set; }
    public bool Secret { get; set; }
}
```

## Exceptions

### CommandException

Exit contract for `RunAsync`:

| Outcome | Exit code |
|---|---|
| `Run` returns normally (also `--help` / `--version`) | `0` |
| Usage / validation error (`UnknownOption`, `MissingRequired`, `RequiresSubcommand`, `InvalidValue`, `UnknownCommand`; `DuplicateOption` is reserved and never thrown — valued repeats resolve last-wins) | `2` |
| Unexpected fault (`Fault`, `NoImplementation`, or `Run` throwing `CommandException` with a custom code) | `1` or `ex.ExitCode` (custom host-code passthrough preserved) |
| Cancellation (`CancellationToken` / Ctrl+C) | `130` (128 + SIGINT) |

Help/footer contract: every help screen (global and per-command) lists `-V, --version` under `GLOBAL OPTIONS:` (`src/ApplicationBuilderHelpers/CommandLineParser/HelpContentProvider.cs:105-110,220-225`); usage-error footers hint at both `--help` and `--version` (`src/ApplicationBuilderHelpers/Exceptions/CommandErrorFooter.cs:28-55`), except when the failing invocation already contained `--help`/`-h`, when only the `--version` hint survives (#509), while `Fault`/`NoImplementation` keep the single-sentence `--help`-only footer (`CommandErrorFooter.cs:56-57`).

```csharp
public enum CommandErrorKind
{
    Fault,
    UnknownOption,
    MissingRequired,
    RequiresSubcommand,
    InvalidValue,
    UnknownCommand,
    DuplicateOption, // Reserved for compatibility; never thrown — valued repeats resolve last-wins.
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

### NoConfigValueException

```csharp
public class NoConfigValueException(string configName) : Exception
```

## ConfigurationExtensions

```csharp
public static class ConfigurationExtensions
{
    public static bool TryGetRefValue(this IConfiguration config, string varName, out string? value);
    public static bool ContainsRefValue(this IConfiguration config, string varName);
    public static string GetRefValue(this IConfiguration config, string varName);
    public static string? GetRefValueOrDefault(this IConfiguration config, string varName, string? defaultValue = null);
}
```

## Abstract Base Classes

### CommandTypeParser\<T\>

```csharp
public abstract class CommandTypeParser<T> : ICommandTypeParser
{
    public Type Type { get; }
    // Override these:
    public abstract T? ParseValue(string? value, out string? validateError);
    public abstract string? GetStringValue(T? value);
    public abstract T? GetDefaultValue();
    public abstract Array CreateTypedArray(int length);
    public virtual IList CreateTypedList(int capacity) => new List<T>(capacity);
}
```
