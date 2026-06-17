# Initialize ApplicationBuilderHelpers C# Project

You are initializing a C# project using the **ApplicationBuilderHelpers** framework — a dependency injection and application lifecycle framework built on clean architecture principles. It provides `ApplicationDependency` lifecycle management, command hierarchy with CLI option parsing, multi-source configuration with `@ref:` chains, build-time encrypted embedded config, source-generated build constants, structured logging, and a middleware pipeline.

Refer to the [ApplicationBuilderHelpers](https://github.com/nicenemo/ApplicationBuilderHelpers) repository for the NuGet package and core documentation. Use this template repository as the reference implementation.

---

## Step 1: Install the Rules and Documentation First

Before creating any application file or running any command, install the project's rule set. These rules dictate folder structure, naming, layering, lifetimes, and prohibited patterns. Every step that follows depends on them.

Clone this template repository into a temporary directory:

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

Read every file in `.github/instructions/` before writing any code. Treat them as MUST/NEVER constraints, not suggestions:

- `project-context.instructions.md` — terminology, project status, breaking-change policy
- `rule-style.instructions.md` — how rules are written
- `architecture.instructions.md` — layering, folder structure, DI lifetimes, ports/adapters, ApplicationDependency, ServiceCollectionExtensions, ConfigurationExtensions, commands, workers, naming, prohibited patterns
- `code-quality.instructions.md` — nullable handling, commenting, constructors, fix hygiene
- `documentation.instructions.md` — when to update docs
- `workflow.instructions.md` — build/test commands, pre-commit checks
- `agent-terminal.instructions.md` — terminal usage rules (no `&&`, `|`, `;`, redirections)
- `ui-test-practices.instructions.md` — test conventions, assertions, no `Task.Delay`

---

## Step 3: Plan the Structure From the Rules

Using the rules from Step 2, design the project layout BEFORE writing files. The architecture rules require a layered structure:

```
<project_root>/
├── .github/
│   └── instructions/                  <- already copied in Step 1
├── AGENTS.md                          <- already copied in Step 1
├── src/
│   ├── Domain/                        <- pure business logic, no I/O
│   │   ├── Domain.cs                  <- ApplicationDependency
│   │   ├── Domain.csproj
│   │   ├── Shared/                    <- base interfaces, models, exceptions
│   │   │   ├── Interfaces/           <- IUnitOfWork, IAggregateRoot, IDomainEvent, IEntity
│   │   │   ├── Models/               <- Entity, AggregateRoot, ValueObject, DomainEvent
│   │   │   ├── Exceptions/           <- DomainException, EntityNotFoundException, ValidationException
│   │   │   ├── Constants/            <- EmptyCollections, shared constants
│   │   │   └── Extensions/
│   │   └── {Feature}/                <- entities, value objects, events, domain services, repository/UoW interfaces
│   ├── Domain.SourceGenerators/       <- source generators (BuildConstantsGenerator)
│   │   └── Domain.SourceGenerators.csproj
│   ├── Application/                   <- interfaces, services, workers, event handlers
│   │   ├── Application.cs             <- ApplicationDependency
│   │   ├── Application.csproj
│   │   └── {Feature}/
│   │       ├── Interfaces/
│   │       │   ├── Inbound/          <- I{Feature}Service (called by Presentation)
│   │       │   └── Outbound/         <- I{External}Provider (called by Application)
│   │       ├── Services/
│   │       ├── Workers/
│   │       ├── EventHandlers/
│   │       ├── Models/
│   │       └── Extensions/
│   ├── Infrastructure.{Provider}/     <- adapters, repositories
│   │   ├── {Provider}Infrastructure.cs <- ApplicationDependency
│   │   ├── {Provider}.Infrastructure.csproj
│   │   ├── Adapters/
│   │   ├── Repositories/
│   │   └── Extensions/
│   ├── Presentation/                  <- shared command base, contracts, components
│   │   ├── Presentation.csproj
│   │   └── Commands/
│   │       └── BaseCommand.cs
│   └── Presentation.Cli/              <- executable composition root
│       ├── Presentation.Cli.csproj
│       ├── Program.cs                 <- ONLY file that imports from Infrastructure
│       └── Commands/
│           └── MainCommand.cs
├── tests/
│   ├── Domain.UnitTests/
│   └── Application.UnitTests/
├── Directory.Build.targets            <- auto-wires BuildConstantsGenerator, embedded config
├── global.json                        <- SDK version pinning
└── ApplicationBuilderHelpersTemplate.slnx
```

For a minimal starter, only `Domain`, `Application`, `Presentation`, and `Presentation.Cli` are required. Infrastructure projects are added as data access or external service needs arrive.

---

## Step 4: Create global.json

Create `global.json` at the project root to pin the .NET SDK version:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestFeature"
  }
}
```

Run `dotnet --list-sdks` to find the installed version and adjust accordingly. Use `latestFeature` roll-forward for flexibility within the same feature band.

---

## Step 5: Create the Solution File (.slnx)

Create `YourSolution.slnx` using the XML-based format:

```xml
<Solution>
  <Project Path="src/Domain/Domain.csproj" />
  <Project Path="src/Domain.SourceGenerators/Domain.SourceGenerators.csproj" />
  <Project Path="src/Application/Application.csproj" />
  <Project Path="src/Presentation/Presentation.csproj" />
  <Project Path="src/Presentation.Cli/Presentation.Cli.csproj" />
</Solution>
```

The `.slnx` format is supported since .NET 9.0.200 SDK and Visual Studio 17.13+. It eliminates GUIDs and configuration boilerplate from the old `.sln` format.

---

## Step 6: Create Directory.Build.targets

Create `Directory.Build.targets` at the project root. This file auto-wires the `BuildConstantsGenerator` source generator and embedded config for leaf executable projects:

```xml
<Project>
	<PropertyGroup Condition="'$(BaseCommandType)' != ''">
		<GenerateBuildConstants>true</GenerateBuildConstants>
	</PropertyGroup>

	<PropertyGroup Condition="'$(GenerateBuildConstants)' == 'true'">
		<AssemblyName Condition="'$(AssemblyName)' == ''">sampleapp</AssemblyName>
		<AssemblyTitle Condition="'$(AssemblyTitle)' == ''">Sample Application</AssemblyTitle>
		<Description Condition="'$(Description)' == ''">Sample Application</Description>
		<FullVersion Condition="'$(FullVersion)' == ''">0.0.0-prerelease.0+build.local</FullVersion>
		<InformationalVersion Condition="'$(InformationalVersion)' == ''">$(FullVersion)</InformationalVersion>
		<AppTag Condition="'$(AppTag)' == ''">prerelease</AppTag>
		<EmbeddedConfigFileName Condition="'$(EmbeddedConfigFileName)' == ''">embedded-config.json</EmbeddedConfigFileName>
		<EmbeddedConfigPath Condition="'$(EmbeddedConfigPath)' == ''">$(MSBuildThisFileDirectory)$(EmbeddedConfigFileName)</EmbeddedConfigPath>
		<BaseCommandType Condition="'$(BaseCommandType)' == ''"></BaseCommandType>
	</PropertyGroup>

	<ItemGroup Condition="'$(GenerateBuildConstants)' == 'true'">
		<ProjectReference Include="$(MSBuildThisFileDirectory)src\Domain.SourceGenerators\Domain.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
		<AdditionalFiles Include="$(EmbeddedConfigPath)" />
		<CompilerVisibleProperty Include="GenerateBuildConstants" />
		<CompilerVisibleProperty Include="BaseCommandType" />
		<CompilerVisibleProperty Include="AssemblyName" />
		<CompilerVisibleProperty Include="AssemblyTitle" />
		<CompilerVisibleProperty Include="Description" />
		<CompilerVisibleProperty Include="FullVersion" />
		<CompilerVisibleProperty Include="AppTag" />
		<CompilerVisibleProperty Include="EmbeddedConfigFileName" />
	</ItemGroup>
</Project>
```

---

## Step 7: Create the Domain Layer

### Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<ItemGroup>
		<PackageReference Include="ApplicationBuilderHelpers" Version="4.1.86" />
		<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.9" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Domain.SourceGenerators\Domain.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
	</ItemGroup>

</Project>
```

### Domain.cs (ApplicationDependency)

The Domain ApplicationDependency registers shared and feature services:

```csharp
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public class Domain : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        // Register shared domain services and feature services
        services.AddSharedServices();
        // services.Add{Feature}Services();
    }
}
```

### Domain/Shared/Interfaces/ (Base Contracts)

Create the marker interfaces in `Domain/Shared/Interfaces/`:

```csharp
// IEntity.cs
namespace Domain.Shared.Interfaces;

public interface IEntity;

// IAggregateRoot.cs
namespace Domain.Shared.Interfaces;

public interface IAggregateRoot;

// IDomainEvent.cs
namespace Domain.Shared.Interfaces;

public interface IDomainEvent;

// IUnitOfWork.cs
namespace Domain.Shared.Interfaces;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

### Domain/Shared/Models/ (Base Classes)

Create the base model classes per the architecture rules. Use `protected` constructors and static factory methods for Entities and ValueObjects.

### Domain/Shared/Extensions/SharedServiceCollectionExtensions.cs

```csharp
namespace Domain.Shared.Extensions;

public static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        return services;
    }
}
```

---

## Step 8: Create the Domain.SourceGenerators Project

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>netstandard2.0</TargetFramework>
		<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" PrivateAssets="all" />
	</ItemGroup>

</Project>
```

Copy the `BuildConstantsGenerator` from the template's `src/Domain.SourceGenerators/Generators/` directory. This source generator creates `Build.Constants`, `Build.ApplicationConstants`, and `Build.BaseCommand<T>` at compile time.

---

## Step 9: Create the Application Layer

### Application.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
		<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.9" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Domain\Domain.csproj" />
	</ItemGroup>

</Project>
```

### Application.cs (ApplicationDependency)

```csharp
using Application.Shared.Extensions;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

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

### Application/Shared/Extensions/SharedServiceCollectionExtensions.cs

```csharp
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

---

## Step 10: Create the Presentation Layer

### Presentation.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<ItemGroup>
		<ProjectReference Include="..\Application\Application.csproj" />
	</ItemGroup>

</Project>
```

### Presentation/Commands/BaseCommand.cs

The base command wires configuration, logging, and the application banner:

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

    [CommandOption(
        'l', "log-level",
        EnvironmentVariable = "LOG_LEVEL",
        Description = "Level of logs to show.")]
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

    protected override async ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        using var scope = applicationHost.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BaseCommand<THostApplicationBuilder>>>();
        logger.LogInformation("Application started: {AppName} v{Version}", ApplicationConstants.AppName, ApplicationConstants.Version);
    }

    private static string BuildAppBanner()
    {
        // Application banner display
        return "Application Banner";
    }
}
```

---

## Step 11: Create the Composition Root (Presentation.Cli)

### Presentation.Cli.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<BaseCommandType>Presentation.Commands.BaseCommand</BaseCommandType>
		<OutputType>Exe</OutputType>
	</PropertyGroup>

	<PropertyGroup>
		<AssemblyName>myapp</AssemblyName>
		<AssemblyTitle>My Application</AssemblyTitle>
		<Description>My CLI application.</Description>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\Application\Application.csproj" />
		<ProjectReference Include="..\Infrastructure.InMemory\Infrastructure.InMemory.csproj" />
		<ProjectReference Include="..\Presentation\Presentation.csproj" />
	</ItemGroup>

</Project>
```

Setting `BaseCommandType` to `Presentation.Commands.BaseCommand` automatically enables `GenerateBuildConstants` in `Directory.Build.targets`, which generates `Build.BaseCommand<T>`.

### Presentation.Cli/Commands/MainCommand.cs

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Presentation.Cli.Commands;

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
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MainCommand>>();

        logger.LogInformation("Hello from MainCommand!");

        // Signal completion for one-shot CLI commands
        cancellationTokenSource.Cancel();
    }
}
```

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

---

## Step 12: Create the Infrastructure Layer

Only create Infrastructure when you need data access or external service calls. For an in-memory starter:

```csharp
// Infrastructure.InMemory/InMemoryInfrastructure.cs
using ApplicationBuilderHelpers;
using Infrastructure.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;

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

Per the architecture rules:
- ApplicationDependency MUST extend `ApplicationDependency` directly
- ApplicationDependency MUST register services through ServiceCollectionExtensions
- ApplicationDependency MUST call `base.Method()` in all lifecycle method overrides

---

## Step 13: Create embedded-config.json

Create `embedded-config.json` at the solution root for build-time encrypted configuration:

```json
{
    "shared_config": {},
    "environment_config": {}
}
```

Generate the encrypted version:

```bash
dotnet build src/Presentation.Cli
```

This file is read by `Directory.Build.targets` as an MSBuild `AdditionalFiles` item, and the `BuildConstantsGenerator` source generator bakes the contents into `Build.ApplicationConstants`.

---

## Step 14: Verify the Setup

The project structure MUST look like this:

```
<project_root>/
├── .github/
│   └── instructions/
│       ├── agent-terminal.instructions.md
│       ├── architecture.instructions.md
│       ├── code-quality.instructions.md
│       ├── documentation.instructions.md
│       ├── project-context.instructions.md
│       ├── rule-style.instructions.md
│       ├── ui-test-practices.instructions.md
│       └── workflow.instructions.md
├── AGENTS.md
├── src/
│   ├── Domain/
│   │   ├── Domain.cs
│   │   ├── Domain.csproj
│   │   └── Shared/
│   ├── Domain.SourceGenerators/
│   │   └── Domain.SourceGenerators.csproj
│   ├── Application/
│   │   ├── Application.cs
│   │   ├── Application.csproj
│   │   └── Shared/
│   ├── Infrastructure.InMemory/
│   │   ├── InMemoryInfrastructure.cs
│   │   └── Infrastructure.InMemory.csproj
│   ├── Presentation/
│   │   ├── Presentation.csproj
│   │   └── Commands/
│   │       └── BaseCommand.cs
│   └── Presentation.Cli/
│       ├── Presentation.Cli.csproj
│       ├── Program.cs
│       └── Commands/
│           └── MainCommand.cs
├── tests/
├── Directory.Build.targets
├── embedded-config.json
├── global.json
└── YourSolution.slnx
```

Build and run:

```bash
dotnet build
```

```bash
dotnet run --project src/Presentation.Cli
```

Expected: the application starts, displays the app banner, logs startup information, and exits.

---

## Step 15: Pre-Commit Verification

Per `workflow.instructions.md`:

```bash
dotnet build
```

MUST complete with 0 warnings and 0 errors.

```bash
dotnet test
```

MUST pass 100%.

---

## Reminder: Rules Take Precedence

When adding any feature beyond this scaffold, MUST consult the rules in `.github/instructions/` first. The architecture rules govern:

- Where each type of file lives (entities, value objects, services, adapters, etc.)
- Which layer may reference which other layer (Domain→nothing, Application→Domain, Infrastructure→Domain+Application, Presentation→Domain+Application)
- Which lifetime each service uses (Domain services: Singleton, Application services: typically Scoped)
- Where interfaces live (Inbound, Outbound, or internal)
- Naming conventions for types, files, and namespaces
- Prohibited patterns that MUST NEVER be introduced

Do not improvise structure. The rules are the contract.
