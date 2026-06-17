# Convert Existing C# Project to ApplicationBuilderHelpers

You are converting an existing C# project to use the **ApplicationBuilderHelpers** framework — a dependency injection and application lifecycle framework built on clean architecture principles. It provides `ApplicationDependency` lifecycle management, command hierarchy with CLI option parsing, multi-source configuration with `@ref:` chains, build-time encrypted embedded config, source-generated build constants, structured logging, and a middleware pipeline.

Refer to the [ApplicationBuilderHelpers](https://github.com/nicenemo/ApplicationBuilderHelpers) repository for the NuGet package and core documentation. Use [ApplicationBuilderHelpersTemplate](https://github.com/Kiryuumaru/ApplicationBuilderHelpersTemplate) as the reference implementation.

---

## Step 1: Install the Rules and Documentation First

Before changing any existing file, install the project's rule set. These rules determine how to refactor the existing code into the layered architecture. Every step that follows depends on them.

Clone the template repository into a temporary directory:

```bash
git clone https://github.com/Kiryuumaru/ApplicationBuilderHelpersTemplate.git /tmp/abht_source
```

Copy the agent instruction files into the project:

```bash
mkdir -p .github/instructions
cp /tmp/abht_source/.github/instructions/*.md .github/instructions/
```

Copy the AGENTS.md into the project:

```bash
cp /tmp/abht_source/AGENTS.md .
```

---

## Step 2: Read All Rules End-to-End

Read every file in `.github/instructions/` before refactoring any code. Treat them as MUST/NEVER constraints, not suggestions:

- `project-context.instructions.md` — terminology, project status (this project is unreleased; refactor freely), breaking-change policy
- `rule-style.instructions.md` — how rules are written
- `architecture.instructions.md` — layering, folder structure, DI lifetimes, ports/adapters, ApplicationDependency, ServiceCollectionExtensions, ConfigurationExtensions, commands, workers, naming, prohibited patterns
- `code-quality.instructions.md` — nullable handling, commenting, constructors, fix hygiene
- `documentation.instructions.md` — when to update docs
- `workflow.instructions.md` — build/test commands, pre-commit checks
- `agent-terminal.instructions.md` — terminal usage rules (no `&&`, `|`, `;`, redirections)
- `ui-test-practices.instructions.md` — test conventions, assertions, no `Task.Delay`

---

## Step 3: Audit the Existing Project Against the Rules

Map the existing code to the rules from Step 2. Produce an audit before changing anything:

1. **Identify the entry point** — This becomes the composition root in `Presentation.Cli/Program.cs`. Look for `Main()` methods, top-level statements, `Program.cs`, or the application startup logic.

2. **Identify each existing type** and classify it per the `architecture.instructions.md` File Placement table:

   | Current Type | Layer | Target Location |
   |-------------|-------|-----------------|
   | Entities with identity | Domain | `Domain/{Feature}/Entities/` |
   | Immutable value types | Domain | `Domain/{Feature}/ValueObjects/` |
   | Pure business logic (no I/O) | Domain | `Domain/{Feature}/Services/` |
   | Data transfer objects | Domain | `Domain/{Feature}/Models/` |
   | Enumerations | Domain | `Domain/{Feature}/Enums/` |
   | Orchestration with I/O | Application | `Application/{Feature}/Services/` |
   | Inbound port interfaces | Application | `Application/{Feature}/Interfaces/Inbound/` |
   | Outbound port interfaces | Application | `Application/{Feature}/Interfaces/Outbound/` |
   | Background tasks | Application | `Application/{Feature}/Workers/` |
   | Database/HTTP adapters | Infrastructure | `Infrastructure.{Provider}/Adapters/` |
   | Repositories | Infrastructure | `Infrastructure.{Provider}.{Feature}/Repositories/` |
   | CLI/HTTP entry points | Presentation | `Presentation.Cli/Commands/` |

3. **For each type identified, record:**
   - Which dependencies it needs (these become constructor parameters)
   - Whether an interface exists (if not, one MUST be created per the interface placement rules)
   - Required lifetime per "Dependency Injection Lifetime Rules" (Singleton/Scoped/Transient)

4. **Identify configuration sources** (hardcoded strings, appsettings.json, env vars, custom parsers) — all consolidate through `IConfiguration` with ConfigurationExtensions per the rules.

5. **Identify background tasks, timers, and polling loops** — these become `BackgroundService` workers in `Application/{Feature}/Workers/`.

6. **Identify logging** (`Console.WriteLine`, `ILogger<T>` scattered across layers) — all consolidate through `ILogger<T>` injected via constructor.

---

## Step 4: Plan the Target Structure From the Rules

Using the audit and the rules, design the target layout. The architecture rules require:

```
src/
├── Domain/
│   ├── Domain.cs                    <- ApplicationDependency
│   ├── Domain.csproj
│   ├── Serialization/
│   ├── Shared/
│   │   ├── Interfaces/              <- IUnitOfWork, IAggregateRoot, IDomainEvent, IEntity
│   │   ├── Models/                  <- Entity, AggregateRoot, ValueObject, DomainEvent
│   │   ├── Exceptions/              <- DomainException, EntityNotFoundException, ValidationException
│   │   ├── Constants/               <- EmptyCollections, shared constants
│   │   └── Extensions/
│   └── {Feature}/
│       ├── Entities/
│       ├── ValueObjects/
│       ├── Models/
│       ├── Interfaces/              <- I{Feature}Repository, I{Feature}UnitOfWork
│       ├── Services/                <- pure domain services
│       ├── Events/
│       ├── Exceptions/
│       ├── Constants/
│       └── Extensions/
├── Domain.SourceGenerators/
│   └── Domain.SourceGenerators.csproj
├── Application/
│   ├── Application.cs               <- ApplicationDependency
│   ├── Application.csproj
│   ├── Serialization/
│   ├── Shared/
│   │   ├── Interfaces/              <- IDomainEventDispatcher, IDomainEventHandler
│   │   ├── Interfaces/Inbound/
│   │   ├── Interfaces/Outbound/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Extensions/
│   └── {Feature}/
│       ├── Interfaces/
│       │   ├── Inbound/             <- I{Feature}Service (called by Presentation)
│       │   └── Outbound/            <- I{External}Provider (called by Application)
│       ├── Services/
│       ├── Workers/
│       ├── EventHandlers/
│       ├── Models/
│       ├── Validators/
│       └── Extensions/
├── Infrastructure.{Provider}/
│   ├── {Provider}Infrastructure.cs   <- ApplicationDependency
│   ├── {Provider}.Infrastructure.csproj
│   ├── Adapters/
│   ├── Repositories/
│   ├── Configurations/
│   └── Extensions/
├── Infrastructure.{Provider}.{Feature}/
│   ├── Adapters/
│   ├── Repositories/
│   └── Extensions/
├── Presentation/
│   ├── Presentation.csproj
│   └── Commands/
│       └── BaseCommand.cs
└── Presentation.Cli/
    ├── Presentation.Cli.csproj
    ├── Program.cs                    <- ONLY file that imports from Infrastructure
    └── Commands/
        └── MainCommand.cs
```

Each layer has an `ApplicationDependency` at its root that registers services via `ServiceCollectionExtensions`. `Presentation.Cli/Program.cs` calls `.AddApplication<T>()` for each layer in dependency order.

---

## Step 5: Add the ApplicationBuilderHelpers NuGet Package

Ensure `ApplicationBuilderHelpers` is referenced in `Domain.csproj` (all layers inherit this transitively through Domain):

```xml
<PackageReference Include="ApplicationBuilderHelpers" Version="4.1.86" />
```

Check the latest version on [NuGet](https://www.nuget.org/packages/ApplicationBuilderHelpers) and update accordingly.

Ensure the `TargetFramework` is set. For new projects, use `net10.0`:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

---

## Step 6: Set Up Directory.Build.targets and global.json

Copy `Directory.Build.targets` from the template to the project root. This file auto-wires:
- `BuildConstantsGenerator` source generator (build constants, `Build.BaseCommand<T>`)
- `embedded-config.json` as an MSBuild AdditionalFile
- MSBuild properties exposed to the source generator

Create or update `global.json` to pin the SDK version:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestFeature"
  }
}
```

Create the solution file in `.slnx` format if not already present:

```bash
dotnet new slnx
```

Or write it manually:

```xml
<Solution>
  <Project Path="src/Domain/Domain.csproj" />
  <Project Path="src/Domain.SourceGenerators/Domain.SourceGenerators.csproj" />
  <Project Path="src/Application/Application.csproj" />
  <Project Path="src/Presentation/Presentation.csproj" />
  <Project Path="src/Presentation.Cli/Presentation.Cli.csproj" />
</Solution>
```

---

## Step 7: Define Interfaces Before Refactoring Implementations

For each cross-layer dependency identified in Step 3, create an interface in the correct location per the rules:

| Interface Type | Location | Implemented By |
|---------------|----------|---------------|
| Repository | `Domain/{Feature}/Interfaces/I{Feature}Repository.cs` | Infrastructure |
| Unit of Work | `Domain/{Feature}/Interfaces/I{Feature}UnitOfWork.cs` | Infrastructure |
| Inbound Service | `Application/{Feature}/Interfaces/Inbound/I{Feature}Service.cs` | Application |
| Outbound Provider | `Application/{Feature}/Interfaces/Outbound/I{Provider}.cs` | Infrastructure |
| Internal | `Application/{Feature}/Interfaces/` | Application |

Interface rules:
- MUST be `public` (used across layers)
- MUST be one type per file
- MUST follow `I{PascalCase}` naming
- Inbound interfaces MAY be called by any layer
- Outbound interfaces MUST NOT be called by Presentation
- Internal interfaces MUST NOT be called by Presentation

Update each implementation to:
- Implement its interface
- Accept dependencies via constructor injection (use primary constructors when only storing)
- Drop manual instantiation of dependencies — the DI container resolves them
- Be `internal` (resolved via DI, not constructed directly)

```csharp
// Before
public class EmailService
{
    private readonly SmtpClient _client = new();
    public void Send(string to, string body) { ... }
}

// After — interface in Application/{Feature}/Interfaces/Outbound/
public interface IEmailSender
{
    Task SendAsync(string to, string body, CancellationToken cancellationToken);
}

// After — implementation in Infrastructure.Email/
internal sealed class SmtpEmailSender(IEmailConfiguration config) : IEmailSender
{
    public async Task SendAsync(string to, string body, CancellationToken cancellationToken)
    {
        ...
    }
}
```

---

## Step 8: Create the ApplicationDependency Classes

Every layer (except Presentation) MUST have an `ApplicationDependency` implementation at its project root. These orchestrate DI registration:

```csharp
// src/Domain/Domain.cs
namespace Domain;

public class Domain : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);
        services.AddSharedServices();
        // services.Add{Feature}Services();
    }
}
```

```csharp
// src/Application/Application.cs
namespace Application;

public class Application : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);
        services.AddSharedServices();
        // services.Add{Feature}Services();
    }
}
```

```csharp
// src/Infrastructure.InMemory/InMemoryInfrastructure.cs
namespace Infrastructure.InMemory;

public class InMemoryInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);
        services.AddInMemoryServices();
    }
}
```

ApplicationDependency rules:
- MUST extend `ApplicationDependency` directly
- MUST NOT extend another ApplicationDependency implementation
- MUST register services through ServiceCollectionExtensions
- MUST call `base.Method()` in all lifecycle method overrides

---

## Step 9: Create ServiceCollectionExtensions

For every feature and shared folder, create a corresponding ServiceCollectionExtensions class:

```csharp
// Domain/Shared/Extensions/SharedServiceCollectionExtensions.cs
namespace Domain.Shared.Extensions;

public static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        return services;
    }
}
```

```csharp
// Application/Shared/Extensions/SharedServiceCollectionExtensions.cs
namespace Application.Shared.Extensions;

public static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }
}
```

ServiceCollectionExtensions rules:
- MUST be `internal static` when called only by same-layer ApplicationDependency
- MAY be `public static` when designed for cross-layer use
- MUST have methods named `Add{Feature}Services`
- MUST be one class per feature

---

## Step 10: Convert Configuration to IConfiguration + ConfigurationExtensions

Replace hardcoded values, scattered `appsettings.json` reads, and environment variable access. Create ConfigurationExtensions per the rules:

```csharp
// Application/Logger/Extensions/LoggerConfigurationExtensions.cs
namespace Application.Logger.Extensions;

public static class LoggerConfigurationExtensions
{
    private const string LoggerLevelKey = "RUNTIME_LOGGER_LEVEL";

    extension(IConfiguration configuration)
    {
        public LogLevel LoggerLevel
        {
            get
            {
                var loggerLevel = configuration.GetRefValueOrDefault(LoggerLevelKey, LogLevel.Information.ToString());
                return Enum.Parse<LogLevel>(loggerLevel);
            }
            set => configuration[LoggerLevelKey] = value.ToString();
        }
    }
}
```

ConfigurationExtension rules:
- MUST use `extension(IConfiguration)` block (C# 13+) or `this IConfiguration` syntax
- MUST use private const string for key names
- MUST have property with getter and setter
- MUST use `GetRefValueOrDefault` for reference chain support

Consolidate all configuration access through these extensions. Presentation sets values (from CLI options, env vars), Application and Infrastructure read them.

---

## Step 11: Convert Background Tasks to Workers

Replace `Task.Run()`, `Timer`, `PeriodicTimer`, and `while(true)` loops with `BackgroundService` implementations in the Application layer:

```csharp
// Application/{Feature}/Workers/ItemProcessor.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Feature.Workers;

internal sealed class ItemProcessor(IServiceScopeFactory scopeFactory, ILogger<ItemProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            // Resolve services and do work
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

Worker rules:
- MUST be `internal` classes (not injectable, resolved as hosted services)
- MUST extend `BackgroundService`
- MUST live in `Application/{Feature}/Workers/`
- MUST create scopes to resolve Scoped services
- MUST NOT be injected into other services

---

## Step 12: Convert Logging to ILogger\<T\>

Replace `Console.WriteLine()`, direct `ILoggerFactory` usage, and static loggers. Inject `ILogger<T>` via constructor:

```csharp
// Before
Console.WriteLine($"Processing order {orderId}");

// After — inject ILogger<OrderService>
internal sealed class OrderService(ILogger<OrderService> logger) : IOrderService
{
    public async Task ProcessAsync(Guid orderId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing order {OrderId}", orderId);
    }
}
```

Logging rules:
- MUST inject `ILogger<T>` via constructor, never create at static scope
- MUST use structured logging with named placeholders
- MUST NOT log sensitive data (API keys, passwords, tokens)

---

## Step 13: Create the Presentation Layer

### Presentation/Commands/BaseCommand.cs

Create a shared base command that all leaf commands extend:

```csharp
using Application.Logger.Extensions;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Presentation.Commands;

public abstract class BaseCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : Command<THostApplicationBuilder>
    where THostApplicationBuilder : IHostApplicationBuilder
{
    public abstract IApplicationConstants ApplicationConstants { get; }

    [CommandOption('l', "log-level", EnvironmentVariable = "LOG_LEVEL", Description = "Level of logs to show.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);
        configuration.LoggerLevel = LogLevel;
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddSingleton(ApplicationConstants);
        services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(applicationBuilder.Configuration.LoggerLevel);
            builder.AddConsole();
        });

        base.AddServices(applicationBuilder, services);
    }
}
```

---

## Step 14: Wire the Composition Root

### Presentation.Cli/Program.cs

`Program.cs` is the ONLY file permitted to import from `Infrastructure.*`:

```csharp
using Infrastructure.InMemory;
using Presentation.Cli.Commands;

return await ApplicationBuilderHelpers.ApplicationBuilder.Create()
    .AddApplication<Domain.Domain>()
    .AddApplication<Application.Application>()
    .AddApplication<InMemoryInfrastructure>()
    .AddCommand<MainCommand>()
    .RunAsync(args);
```

Layer registration order MUST be: Domain → Application → Infrastructure → Commands.

### Presentation.Cli/Commands/MainCommand.cs

Move the existing entry point logic into a command:

```csharp
[Command("Main subcommand.")]
internal class MainCommand : Build.BaseCommand<HostApplicationBuilder>
{
    protected override ValueTask<HostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = Host.CreateApplicationBuilder();
        return new ValueTask<HostApplicationBuilder>(builder);
    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        await base.Run(applicationHost, cancellationTokenSource);

        using var scope = applicationHost.Services.CreateScope();
        // Resolve services and perform work

        cancellationTokenSource.Cancel(); // Signal completion for one-shot commands
    }
}
```

### Presentation.Cli.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <BaseCommandType>Presentation.Commands.BaseCommand</BaseCommandType>
        <OutputType>Exe</OutputType>
    </PropertyGroup>

    <PropertyGroup>
        <AssemblyName>myapp</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\Application\Application.csproj" />
        <ProjectReference Include="..\Infrastructure.InMemory\Infrastructure.InMemory.csproj" />
        <ProjectReference Include="..\Presentation\Presentation.csproj" />
    </ItemGroup>

</Project>
```

---

## Step 15: Convert the Solution to .slnx Format

If the project uses the old `.sln` format, convert to `.slnx`:

```bash
dotnet sln migrate
```

Or create the `.slnx` manually with clean XML structure. The `.slnx` format eliminates GUIDs and configuration platform boilerplate.

---

## Step 16: Verify the Conversion

1. Every layer has an `ApplicationDependency` class at its root
2. All dependencies are injected via constructor — no manual instantiation remains
3. All cross-layer dependencies use interfaces in the correct `Interfaces/Inbound/` or `Interfaces/Outbound/` folders
4. All background tasks use `BackgroundService` (zero `while(true)` outside Workers)
5. All configuration access goes through `IConfiguration` with ConfigurationExtensions
6. All logging uses `ILogger<T>`
7. Only `Presentation.Cli/Program.cs` imports from `Infrastructure.*`
8. Each layer registers services through ServiceCollectionExtensions, called from ApplicationDependency
9. `Presentation.Cli.csproj` has `BaseCommandType` set and `OutputType` as `Exe`
10. The solution uses `.slnx` format

Build:

```bash
dotnet build
```

MUST complete with 0 warnings and 0 errors.

Run:

```bash
dotnet run --project src/Presentation.Cli
```

Expected: the application starts, logs via `ILogger<T>`, and exits gracefully (with Ctrl+C for long-running commands).

---

## Step 17: Pre-Commit Verification

Per `workflow.instructions.md`:

```bash
dotnet build
```

MUST complete with 0 warnings, 0 errors.

```bash
dotnet test
```

MUST pass 100%.

---

## Conversion Checklist

- [ ] Agent instruction files copied to `.github/instructions/` (Step 1)
- [ ] AGENTS.md copied to project root (Step 1)
- [ ] All rules read end-to-end (Step 2)
- [ ] Existing code audited and classified by layer (Step 3)
- [ ] Target structure planned per architecture rules (Step 4)
- [ ] ApplicationBuilderHelpers NuGet package added (Step 5)
- [ ] Directory.Build.targets and global.json configured (Step 6)
- [ ] Interfaces defined for every cross-layer dependency (Step 7)
- [ ] ApplicationDependency classes created for each layer (Step 8)
- [ ] ServiceCollectionExtensions created for each feature/shared (Step 9)
- [ ] Configuration consolidated through IConfiguration + ConfigurationExtensions (Step 10)
- [ ] Background tasks converted to BackgroundService Workers (Step 11)
- [ ] Logging consolidated through ILogger\<T\> (Step 12)
- [ ] BaseCommand created in Presentation layer (Step 13)
- [ ] Composition root wired in Presentation.Cli/Program.cs (Step 14)
- [ ] Solution converted to .slnx format (Step 15)
- [ ] Application builds with 0 warnings (Step 16)
- [ ] Application starts and exits gracefully (Step 16)
- [ ] Pre-commit checks pass (Step 17)

---

## Reminder: Rules Take Precedence

The rules in `.github/instructions/` govern every refactor decision:

- Where each type of file lives (entities, value objects, services, adapters, etc.)
- Which layer may reference which other layer
- Which lifetime each service uses
- Where each interface lives (Inbound, Outbound, or internal per feature)
- Naming conventions for types, files, and namespaces
- Prohibited patterns that MUST NEVER be introduced (no `using Infrastructure.*` outside Program.cs, no `public set` on entity properties, no I/O in Domain services, etc.)

Do not improvise structure. The rules are the contract.
