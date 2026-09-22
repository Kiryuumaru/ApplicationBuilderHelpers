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
   (`CommandExecutor.ExecuteCommand` adds `command` then the global
   dependencies, then calls `ApplicationHostBuilder.BuildInternal`), and `BuildInternal` then invokes
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

Stage-to-file map (condensed; canonical 7-row map in `docs/commands.md` lifecycle stages):

- Steps 1–2 → thin sequencer `CommandExecutor` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandExecutor.cs:19-33`, `ExecuteCommand :58-106`; build at `:75-81`) over `CommandShutdownScope` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandShutdownScope.cs:23-46`, linked CTS outer+CtrlC only, `IsExternalAbort` at `:67-68` / `ThrowIfExternalAbort` at `:74-75`).
- Step 3 → `ServiceInjectionGate.Inject` (`src/ApplicationBuilderHelpers/CommandLineParser/ServiceInjectionGate.cs:111`; fail-fast helper at `:133-139`) from the per-command scope (`CommandExecutor.cs:84-86`); injectable cancel signal `IConsoleCancelSignal` (`src/ApplicationBuilderHelpers/CommandLineParser/IConsoleCancelSignal.cs:14-26`) / `ConsoleCancelSignal` (`src/ApplicationBuilderHelpers/CommandLineParser/ConsoleCancelSignal.cs:12-38`) over adapter-only `ConsoleOutput` (`src/ApplicationBuilderHelpers/CommandLineParser/ConsoleOutput.cs:31-35`).
- Step 4 → joint run `CommandRunOrchestrator` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOrchestrator.cs:23-99`; `Exiting` at `:52,:78,:88`) returning `CommandRunOutcome` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandRunOutcome.cs:8-41`) plus executor trailing success `Exiting` (`CommandExecutor.cs:90-92`) and null-guarded `Exited` `finally` (`:94-99`); single cancel-wins point `CommandExitMapper` (`src/ApplicationBuilderHelpers/CommandLineParser/CommandExitMapper.cs:18-40`, remark at `:13-17`).
- Exit mapping → `CommandExecutor.cs:39` (`130`) + `ExternalCancellationException` at `:45-51`, surfaced at `CommandLineParser.cs:116-119,126-129`; internal-only `OperationCanceledException` stays `0` (`:131-135`).

Fail-safe note: `LifetimeGlobalService` `Interlocked.Exchange` guards (`src/ApplicationBuilderHelpers/Services/LifetimeGlobalService.cs:17-22,58-86`) are retained fail-safe — first caller wins, late callers no-op. Proof: `AbsolutePathAndLifetimeTests.LifetimeGlobalService_ExitingDoubleInvoke_RunsOnce` and `..._ExitedDoubleInvoke_RunsOnce` (`src/ApplicationBuilderHelpers.Test.Cli.UnitTest/AbsolutePathAndLifetimeTests.cs:222-260`).

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
