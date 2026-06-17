# Getting Started

A quick guide to building your first CLI application with ApplicationBuilderHelpers.

## Installation

```bash
dotnet add package ApplicationBuilderHelpers
```

The package targets `net6.0` through `net10.0` and is AOT-compatible.

## Minimal Application

```csharp
// Program.cs
using ApplicationBuilderHelpers;

return await ApplicationBuilder.Create()
    .AddCommand<HelloCommand>()
    .RunAsync(args);
```

`RunAsync` parses command-line arguments and returns `Task<int>` — `0` on success.

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

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        Console.WriteLine($"Hello, {Name}!");
        cts.Cancel();
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

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var greeter = applicationHost.Services.GetRequiredService<IGreetingService>();
        Console.WriteLine(greeter.GetGreeting());
        cts.Cancel();
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

- [Commands](commands.md) — Deep dive into command definitions and attributes
- [Application Dependencies](application-dependencies.md) — Lifecycle hooks and modular composition
- [Configuration & Themes](configuration.md) — Customize help output and behavior
