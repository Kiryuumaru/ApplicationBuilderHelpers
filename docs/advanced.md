# Advanced Topics

## Sub-Commands

Build hierarchical command structures with space-separated names:

```csharp
[Command("deploy", description: "Deployment operations")]
public class DeployCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        Console.WriteLine("Use a sub-command: deploy prod, deploy staging");
        cts.Cancel();
        return ValueTask.CompletedTask;
    }
}

[Command("deploy prod", description: "Deploy to production")]
public class DeployProductionCommand : Command { /* ... */ }

[Command("deploy prod rollback", description: "Rollback production")]
public class DeployProductionRollbackCommand : Command { /* ... */ }
```

```bash
myapp deploy                    # Shows sub-command help
myapp deploy prod               # Runs production deploy
myapp deploy prod rollback      # Runs rollback with 3-part name
```

Arbitrary nesting depth is supported.

## Multiple Host Types

### Console Apps (Default)

```csharp
public class MyCommand : Command  // Uses HostApplicationBuilder
```

### Custom Host Types

```csharp
public class WebCommand : Command<WebApplicationBuilder>
{
    protected override ValueTask<WebApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = WebApplication.CreateBuilder();
        return new ValueTask<WebApplicationBuilder>(builder);
    }

    protected override async ValueTask Run(ApplicationHost<WebApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var app = applicationHost.Builder.Build();
        app.MapGet("/", () => "Hello World");
        await app.RunAsync();
    }
}
```

Any type implementing `IHostApplicationBuilder` is supported.

## Exit Codes

`RunAsync` returns `Task<int>`:

```csharp
int exitCode = await ApplicationBuilder.Create()
    .AddCommand<MyCommand>()
    .RunAsync(args);

Environment.Exit(exitCode);
```

Throw `CommandException` for non-zero exits:

```csharp
throw new CommandException("Configuration missing", exitCode: 2);
```

## Error Handling

The library catches `CommandException` during execution and returns its exit code. Other unhandled exceptions will propagate.

## Help System

Help is automatically generated from command attributes:

```bash
myapp --help       # Global help: lists all commands
myapp deploy --help # Command-specific help: shows options & sub-commands
myapp --version    # Shows version number
```

The `--help` and `--version` flags are handled automatically — you don't need to define them.
