# Plain CLI Template

A starter template for building CLI applications with [ApplicationBuilderHelpers](https://github.com/nicenemo/ApplicationBuilderHelpers), clean architecture, and DDD patterns.

Includes a sample **WeatherForecast** feature to demonstrate the end-to-end flow through all layers.

## Quick Start

```bash
dotnet build
dotnet run --project src/Presentation.Cli
```

## Project Structure

```
src/
├── Domain/                      # Entities, value objects, events, interfaces
├── Domain.SourceGenerators/     # Build constants source generator
├── Application/                 # Services, event handlers, config extensions
├── Infrastructure.InMemory/     # In-memory repository and unit of work
├── Presentation/                # Shared command base
└── Presentation.Cli/            # CLI entry point
tests/
├── Domain.UnitTests/
└── Application.UnitTests/
```

## What the Template Provides

- **Clean Architecture**: Domain → Application → Infrastructure → Presentation layer separation
- **DDD Building Blocks**: Aggregate roots, entities, value objects, domain events, repositories, unit of work
- **ApplicationBuilderHelpers Integration**: `ApplicationDependency` lifecycle, `Command` hierarchy, CLI option parsing
- **Domain Event Dispatch**: `IDomainEvent`, `AggregateRoot.AddDomainEvent()`, `DomainEventDispatcher`, parallel handlers
- **Embedded Config**: Build-time encrypted configuration with runtime decryption
- **Trimming Ready**: Source-generated JSON contexts and `DynamicallyAccessedMembers` annotations
- **NUKE Build**: Build automation with environment-aware configuration

## Sample Feature: WeatherForecast

The included WeatherForecast feature demonstrates how a feature flows through each layer:

| Layer | Component | Role |
|-------|-----------|------|
| Domain | `WeatherForecastEntity` | Aggregate root raising `WeatherForecastCreatedEvent` |
| Domain | `Temperature` | Value object with Celsius/Fahrenheit conversion |
| Application | `WeatherForecastService` | Orchestrates forecast generation, commits via unit of work |
| Application | `LogForecastHandler`, `NotifySubscribersHandler` | Domain event handlers for decoupled side effects |
| Infrastructure | `InMemoryWeatherForecastRepository` | Repository and unit of work implementation |
| Presentation | `MainCommand` | CLI entry point resolving services and displaying output |

Replace or extend this feature with your own domain.

### CLI Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--location` | | `New York` | Location for sample forecast |
| `--days` | `-d` | `5` | Number of days (1-14) |
| `--log-level` | `-l` | `Information` | Log level |

## License

MIT
