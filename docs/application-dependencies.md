# Application Dependencies

`ApplicationDependency` is the base class for application modules. It provides lifecycle hooks that let you configure the application in a structured, predictable order.

## Lifecycle Order

Each method is called on **every** registered dependency in sequence before moving to the next phase:

| Order | Method | When |
|---|---|---|
| 1 | `CommandPreparation(ApplicationBuilder)` | Register type parsers before command parsing |
| 2 | `BuilderPreparation(ApplicationHostBuilder)` | Prepare host builder before config |
| 3 | `AddConfigurations(ApplicationHostBuilder, IConfiguration)` | Bind config sections, add providers |
| 4 | `AddServices(ApplicationHostBuilder, IServiceCollection)` | Register DI services |
| 5 | `AddMiddlewares(ApplicationHost, IHost)` | Configure the middleware pipeline |
| 6 | `AddMappings(ApplicationHost, IHost)` | Define endpoint mappings (web hosts) |
| 7 | `RunPreparation(ApplicationHost)` | Sync pre-run setup — all deps run in parallel |
| 8 | `RunPreparationAsync(ApplicationHost, CancellationToken)` | Async pre-run setup — all deps run in parallel |

## Example

```csharp
public class CoreApplication : ApplicationDependency
{
    public override void CommandPreparation(ApplicationBuilder applicationBuilder)
    {
        applicationBuilder.AddCommandTypeParser<CustomTypeParser>();
    }

    public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
    {
        // Access builder.Configuration, builder.Services, etc.
    }

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        applicationBuilder.Services.Configure<AppSettings>(configuration.GetSection("App"));
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
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
        // Quick sync setup — runs in parallel with other deps
    }

    public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        // Async setup — runs in parallel with other deps
        return ValueTask.CompletedTask;
    }
}
```

## Registration

```csharp
// By type (parameterless constructor required)
ApplicationBuilder.Create()
    .AddApplication<CoreApplication>()
    // ...

// By instance
var app = new CoreApplication();
ApplicationBuilder.Create()
    .AddApplication(app)
    // ...
```

## ApplicationHostBuilder Access

`ApplicationHostBuilder` provides:

- `Builder` — The underlying `IHostApplicationBuilder`
- `Services` — The `IServiceCollection`
- `Configuration` — The `IConfiguration`
- Enumerates all registered `IApplicationDependency` instances

## Executor Service Ordering

The executor (`CommandExecutor`, between `BuildInternal` and `RunInternal`)
runs these steps in order:

1. The target command's registrations run first: the command instance is
   added to the dependency list before the global dependencies
   (`CommandExecutor` :86-90), and `BuildInternal` then invokes
   `BuilderPreparation` → `AddConfigurations` → `AddServices` in dependency
   order — so the command's `AddServices` runs before the globals', and a
   later registration for the same service wins (last-wins, no warn/fail
   diagnostic).
2. The host is built once per command run.
3. One `IServiceScope` is created from the built host's
   `IServiceScopeFactory`; `[FromServices]` / `[FromKeyedServices]`
   properties on the command are injected from `scope.ServiceProvider`
   (after CLI binding, disjoint from CLI-bound properties).
4. The joint command/host run executes; the scope is disposed after the
   lifetime callbacks.

Guidance:

- Prefer `TryAdd*` (`TryAddSingleton`, `TryAddScoped`, …) in shared
  dependencies when a command is allowed to override a registration, so the
  override stays intentional.
- Never resolve per-command state from the root `Services` provider and
  hold it: scoped services belong to the per-command scope, which is
  disposed after the run. Inside `Run`, prefer the already-injected
  properties; use `applicationHost.Services.CreateScope()` for any
  additional scoped resolutions.

## ApplicationHost Access

`ApplicationHost` provides:

- `Host` — The built `IHost`
- `Services` — The `IServiceProvider`
- `Builder` — The original `IHostApplicationBuilder`
