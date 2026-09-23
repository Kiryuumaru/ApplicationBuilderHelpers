# Getting Started

A quick guide to building your first CLI application with ApplicationBuilderHelpers.

## Installation

```bash
dotnet add package ApplicationBuilderHelpers
```

The package targets `net6.0` through `net10.0` and is AOT-compatible and trimmable.

## Minimal Application

```csharp
// Program.cs
using ApplicationBuilderHelpers;

return await ApplicationBuilder.Create()
    .AddCommand<HelloCommand>()
    .RunAsync(args);
```

`RunAsync` parses command-line arguments and returns `Task<int>` — `0` on success (also `--help` / `--version`), `2` on usage errors, `1` (or a custom code) on faults, `130` on cancellation. See the [API Reference](api-reference.md#commandexception) exit contract.

## Your First Command

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;

[Command(description: "Say hello")]
public class HelloCommand : Command
{
    [CommandArgument(Name = "name", Position = 0, Description = "Who to greet")]
    public string Name { get; set; } = "World";

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}
```

Run it:

```bash
dotnet run -- Alice
# Hello, Alice!

dotnet run --
# Hello, World!
```

## Adding Services

Commands can register their own services:

```csharp
[Command("greet", description: "Greet with a service")]
public class GreetCommand : Command
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddSingleton<IGreetingService, GreetingService>();
    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        var greeter = applicationHost.Services.GetRequiredService<IGreetingService>();
        Console.WriteLine(greeter.GetGreeting());
    }
}
```

## Using Application Modules

Group shared configuration and services in `ApplicationDependency` classes:

```csharp
public class CoreApplication : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
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

## Next Steps

Command topology note: a single `[Command]` with a null `Term` merges at the root and runs on a bare invocation (`SubCommandInfo.FromCommand` at `src/ApplicationBuilderHelpers/CommandLineParser/SubCommandInfo.cs:140-159`). A CLI that registers only leaf subcommands (e.g. only `[Command("greet", ...)]`) has no root implementation — a bare run (`[]`) exits `2` with `'<root>' requires a subcommand` plus the global usage footer, and root `--help` first renders the global model (`src/ApplicationBuilderHelpers/CommandLineParser/ArgumentParser.cs:62-83`; `src/ApplicationBuilderHelpers/CommandLineParser/HelpFormatter.cs:42-44`). See [Advanced Topics](advanced.md#bare-root-and-help-first) for the full matrix.

- [Commands](commands.md) — Deep dive into command definitions and attributes
- [Application Dependencies](application-dependencies.md) — Lifecycle hooks and modular composition
- [Configuration & Themes](configuration.md) — Customize help output and behavior
