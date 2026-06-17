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
| `AddCommand<TCommand>()` | `ApplicationBuilder` | Register a command type |
| `AddApplication<T>()` | `ApplicationBuilder` | Register an application dependency by type |
| `AddApplication(IApplicationDependency)` | `ApplicationBuilder` | Register an application dependency instance |
| `AddCommandTypeParser<T>()` | `ApplicationBuilder` | Register a custom type parser |
| `SetTheme<TTheme>()` | `ApplicationBuilder` | Set theme by type |
| `SetTheme(IConsoleTheme)` | `ApplicationBuilder` | Set theme by instance |
| `SetExecutableName(string)` | `ApplicationBuilder` | Override auto-detected name |
| `SetExecutableTitle(string)` | `ApplicationBuilder` | Override auto-detected title |
| `SetExecutableDescription(string)` | `ApplicationBuilder` | Override auto-detected description |
| `SetExecutableVersion(string)` | `ApplicationBuilder` | Override auto-detected version |
| `SetHelpWidth(int)` | `ApplicationBuilder` | Set help line width |
| `SetHelpBorderWidth(int)` | `ApplicationBuilder` | Set help border indentation |
| `RunAsync(string[], CancellationToken)` | `Task<int>` | Parse args and run |

## Command

```csharp
public abstract class Command : Command<HostApplicationBuilder>
public abstract class Command<THostApplicationBuilder> : ApplicationDependency, ICommand
```

### Abstract Members (to override)

| Member | Returns | Description |
|---|---|---|
| `Run(ApplicationHost<THostApplicationBuilder>, CancellationTokenSource)` | `ValueTask` | Command logic |
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
}
```

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
}
```

## Exceptions

### CommandException

```csharp
public class CommandException : Exception
{
    public int ExitCode { get; }
    public CommandException(int exitCode);
    public CommandException(string message, int exitCode);
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
}
```
