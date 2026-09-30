# Getting Started

Build your first command-line app in five minutes.

## Install the Package

```bash
dotnet add package ApplicationBuilderHelpers
```

Works on `net6.0` through `net10.0`. Ready for trimmed and ahead-of-time builds on `net8.0` and later.

## Minimal App

Create `Program.cs`:

```csharp
// Program.cs
using ApplicationBuilderHelpers;

return await ApplicationBuilder.Create()
    .AddCommand<HelloCommand>()
    .RunAsync(args);
```

Three steps: create a builder, add a command, run it. The library parses the arguments and calls your command.

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

    protected override ValueTask Run(
        ApplicationHost<HostApplicationBuilder> applicationHost,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Hello, {Name}!");
        return ValueTask.CompletedTask;
    }
}
```

What each part does:

- `[Command]` marks the class as a command. With no name it runs at the root.
- `[CommandArgument]` maps the first typed word to `Name`.
- `Run` holds your logic. Return normally for success (exit `0`).

To name a command, pass a name: `[Command("greet", description: "Greet with a service")]`. Use spaces for subcommands: `[Command("deploy prod")]`.

## Run It

```bash
dotnet run -- Alice
# Hello, Alice!
# exit 0

dotnet run --
# Hello, World!
# exit 0
```

Exit codes: `0` means success, `2` means bad input, `1` means failure, `130` means canceled. See [API Reference](api-reference.md).

Use `--` to force words as positionals. Near-miss names get a `Did you mean` hint. See [Commands](commands.md).

## Add a Service

Register services in `AddServices`, then read them in `Run`:

```csharp
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[Command("greet", description: "Greet with a service")]
public class GreetCommand : Command
{
    public override void AddServices(
        ApplicationHostBuilder applicationBuilder,
        IServiceCollection services)
    {
        services.AddSingleton<IGreetingService, GreetingService>();
    }

    protected override ValueTask Run(
        ApplicationHost<HostApplicationBuilder> applicationHost,
        CancellationToken cancellationToken)
    {
        var greeter = applicationHost.Services.GetRequiredService<IGreetingService>();
        Console.WriteLine(greeter.GetGreeting());
        return ValueTask.CompletedTask;
    }
}
```

For automatic property filling with `[FromServices]`, see [Commands](commands.md).

## Share Setup Across Commands

Put common services in a shared module, then add it once:

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

Full setup order lives in [Application Dependencies](application-dependencies.md).

## Next Steps

- [Commands](commands.md) — Add options, arguments, and subcommands
- [Application Dependencies](application-dependencies.md) — Share setup between commands
- [Configuration & Themes](configuration.md) — Name your app and style its help
