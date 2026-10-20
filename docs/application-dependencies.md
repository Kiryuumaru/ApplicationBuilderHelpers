# Application Dependencies

Shared setup modules. Group services, settings, and startup steps once. Reuse them in every command.

## The Idea

A dependency is a class that extends `ApplicationDependency`. Override only the steps your module owns. Register it once with `AddApplication`. Every command then gets the same setup.

Use one when two or more commands need the same services or settings. Keep command-specific setup in the command itself.

## Setup Order

Each step runs on every registered module before the next step starts. Within steps 2–7, the command's own entry runs last, after the modules. Steps 1–7 run in turn. Step 8 runs all modules at once:

| Order | Override | Use it to |
|---|---|---|
| 1 | `CommandPreparation` | Register custom type parsers before parsing |
| 2 | `BuilderPreparation` | Adjust the host builder before settings load |
| 3 | `AddConfigurations` | Add settings sources |
| 4 | `AddServices` | Register services |
| 5 | `AddMiddlewares` | Wire middleware onto the built app |
| 6 | `AddMappings` | Map endpoints (web apps) |
| 7 | `RunPreparation` | Quick sync setup in the joint run with your command |
| 8 | `RunPreparationAsync` | Async setup in the joint run with your command (all modules run at once) |

Steps 5–8 run on the host task. The command and the host run together as one joint run. A synchronous command always finishes first; an asynchronous command interleaves with steps 5–8 — a fast one usually completes first, but the order is timing-dependent. Only step 8 can be canceled mid-flight. A successful command cancels the host task and can silently truncate token-honoring step-8 work while the process still exits `0` when no external cancellation is live.

## Example

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class CoreApplication : ApplicationDependency
{
    public override void CommandPreparation(ApplicationBuilder applicationBuilder)
    {
        applicationBuilder.AddCommandTypeParser<CustomTypeParser>();
    }

    public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
    {
        // Access applicationBuilder.Configuration, applicationBuilder.Services.
    }

    public override void AddConfigurations(
        ApplicationHostBuilder applicationBuilder,
        IConfiguration configuration)
    {
        applicationBuilder.Services.Configure<AppSettings>(configuration.GetSection("App"));
    }

    public override void AddServices(
        ApplicationHostBuilder applicationBuilder,
        IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
    }

    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        // app.UseMiddleware<...>();
    }

    public override void AddMappings(ApplicationHost applicationHost, IHost host)
    {
        // app.MapGet(...);
    }

    public override void RunPreparation(ApplicationHost applicationHost)
    {
        // Quick sync setup.
    }

    public override ValueTask RunPreparationAsync(
        ApplicationHost applicationHost,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

## Register a Module

```csharp
// By type (needs a parameterless constructor)
ApplicationBuilder.Create()
    .AddApplication<CoreApplication>();

// By instance (use when the module needs constructor values)
var app = new CoreApplication();
ApplicationBuilder.Create()
    .AddApplication(app);
```

## What Your Command Can Reach

During setup your module receives an `ApplicationHostBuilder` with:

- `Builder` — the underlying host builder
- `Services` — the service collection
- `Configuration` — the app settings

During `Run` your command receives an `ApplicationHost` with:

- `Host` — the built host
- `Services` — the service provider
- `Builder` — the original host builder

## Practical Guidance

- The command's own `AddServices` runs after shared modules. A later registration for the same service wins. So a command can replace a shared default by registering its own.
- `BuilderPreparation` and `AddServices` run after the default host logging policy, so your logging settings win. See [Advanced Topics](advanced.md#host-logging).
- In shared modules use `TryAdd*` (`TryAddSingleton`, `TryAddScoped`) when a command may replace the registration. The replacement then stays intentional. In the command, override with plain `Add*`; command-side `TryAdd*` fills only types no module registered. When the host consumes a service as an enumerable (`IHostedService`, `IConfigureOptions`, ...), the command's registration reorders rather than overrides; call `services.RemoveAll<T>()` before re-adding.
- Prefer the injected `[FromServices]` shim properties inside `Run` (define the shims once; see [Commands](commands.md)). They come from a scope that closes after the run.
- For extra scoped services inside `Run`, open a scope: `applicationHost.Services.CreateScope()`. Never hold scoped services past the run.
