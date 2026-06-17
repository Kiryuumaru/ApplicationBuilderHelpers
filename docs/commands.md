# Commands

Commands are the core unit of work in ApplicationBuilderHelpers. Each command is a class that processes a specific CLI operation.

## Defining a Command

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

[Command("build", description: "Build the project")]
public class BuildCommand : Command
{
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        // Command logic here
        cts.Cancel();
    }
}
```

### Command Attribute

`[Command]` supports these constructors:

```csharp
[Command(description: "Description only")]          // Auto-detects name from class
[Command("name")]                                     // Name only
[Command("name", description: "With description")]    // Both
```

The `Term` property is the command name. Use space-separated names for sub-commands:

```csharp
[Command("deploy prod", description: "Deploy to production")]
```

### Command Variants

| Base Class | Host Builder | Use For |
|---|---|---|
| `Command` | `HostApplicationBuilder` | Console apps, workers |
| `Command<THostApplicationBuilder>` | Custom | Web apps, custom hosts |

## Options

Define command-line flags with `[CommandOption]`:

```csharp
[CommandOption('v', "verbose", Description = "Enable verbose output")]
public bool Verbose { get; set; }

[CommandOption('c', "config", Description = "Config file path", EnvironmentVariable = "APP_CONFIG")]
public string? ConfigPath { get; set; }

[CommandOption("timeout", Description = "Timeout in seconds")]
public int Timeout { get; set; } = 30;
```

### Option Constructors

```csharp
[CommandOption('s', "long-name")]   // Short + long
[CommandOption('s')]                 // Short only
[CommandOption("long-name")]         // Long only
```

### Option Properties

| Property | Type | Description |
|---|---|---|
| `Term` | `string?` | Long option name (e.g., `"verbose"` → `--verbose`) |
| `ShortTerm` | `char?` | Short option flag (e.g., `'v'` → `-v`) |
| `Description` | `string?` | Help text |
| `EnvironmentVariable` | `string?` | Fallback env var |
| `Required` | `bool` | Must be provided |
| `FromAmong` | `object[]` | Restrict to specific values |
| `CaseSensitive` | `bool` | Case-sensitive matching for FromAmong |

### Restricted Values

```csharp
[CommandOption('l', "level", FromAmong = new[] { "debug", "info", "warn", "error" })]
public string Level { get; set; } = "info";
```

## Arguments

Define positional arguments with `[CommandArgument]`:

```csharp
[CommandArgument(Name = "source", Position = 0, Description = "Source file", Required = true)]
public string SourceFile { get; set; } = "";

[CommandArgument(Name = "dest", Position = 1, Description = "Destination")]
public string? DestPath { get; set; }
```

### Argument Properties

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | Display name in help |
| `Position` | `int` | Positional index |
| `Description` | `string?` | Help text |
| `Required` | `bool` | Must be provided |
| `FromAmong` | `object[]` | Restrict to specific values |
| `CaseSensitive` | `bool` | Case-sensitive matching |

## Accessing Services

Use the service locator from `applicationHost.Services`:

```csharp
protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
{
    var logger = applicationHost.Services.GetRequiredService<ILogger<MyCommand>>();
    var service = applicationHost.Services.GetRequiredService<IMyService>();
    // ...
}
```

## Command Lifecycle

Commands inherit the full `ApplicationDependency` lifecycle. See [Application Dependencies](application-dependencies.md) for details on `AddServices`, `AddConfigurations`, `AddMiddlewares`, `AddMappings`, `RunPreparation`, and `RunPreparationAsync`.

## Exit Codes

Throw `CommandException` for non-zero exit:

```csharp
throw new CommandException("Operation failed", exitCode: 1);
```
