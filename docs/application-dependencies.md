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

## ApplicationHost Access

`ApplicationHost` provides:

- `Host` — The built `IHost`
- `Services` — The `IServiceProvider`
- `Builder` — The original `IHostApplicationBuilder`
