---
applyTo: '**'
---
# Architecture Rules

## Layer Dependencies

```
Domain
  ^
Application
  ^
Application.Server    Application.Client
  ^                     ^
Infrastructure.*      Infrastructure.*
  ^                     ^
Presentation.*.Server Presentation.*.Client
```

```
+-------------------------------------------+
|            PRESENTATION                   |
|  References: Application, Domain          |
|  Infrastructure: Program.cs only          |
+-------------------------------------------+
                    |
                    v
+-------------------------------------------+
|           INFRASTRUCTURE                  |
|  References: Application, Domain          |
|  Implements: Domain interfaces (repos)    |
|  Implements: Application interfaces (ext) |
+-------------------------------------------+
                    |
                    v
+-------------------------------------------+
|            APPLICATION                    |
|  References: Domain only                  |
|  Defines: Service interfaces, handlers    |
+-------------------------------------------+
                    |
                    v
+-------------------------------------------+
|              DOMAIN                       |
|  References: DI helpers only              |
|  Contains: Entities, models, value objects|
+-------------------------------------------+
```

---

## Quick Start

When adding a new feature:

```
1. Domain/{Feature}/
   ├── Create entities, value objects, models, enums
   ├── Define I{Feature}Repository in Interfaces/
   └── Define I{Feature}UnitOfWork in Interfaces/
            ↓
2. Application/{Feature}/
   ├── Define I{Feature}Service in Interfaces/Inbound/
   ├── Define I{External}Provider in Interfaces/Outbound/ (if needed)
   ├── Implement {Feature}Service in Services/
   └── Create {Feature}ServiceCollectionExtensions in Extensions/
            ↓
3. Infrastructure.{Provider}.{Feature}/
   ├── Implement repository in Repositories/
   ├── Implement outbound adapters in Adapters/
   └── Create {Feature}ServiceCollectionExtensions in Extensions/
            ↓
4. Presentation.*/
   ├── Add controller/component (incoming adapter)
   └── Register in Program.cs via .AddApplication<T>()
```

Key principle: **Domain defines contracts → Application orchestrates → Infrastructure implements → Presentation drives**

---

## Layer Reference Rules

Domain:
- MUST NOT reference any other layer (Application, Infrastructure, or Presentation)
- Is the innermost circle containing pure business logic
- MAY reference DI helpers: `Microsoft.Extensions.DependencyInjection.Abstractions`, `ApplicationBuilderHelpers`, `Domain.SourceGenerators`
- MUST NOT have framework attributes or external dependencies beyond DI helpers

Application:
- MUST only reference Domain
- MUST NOT reference Infrastructure or Presentation
- MAY reference core abstractions: `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`
- MAY reference DI helpers: `ApplicationBuilderHelpers`, `Domain.SourceGenerators`
- Defines ports and orchestrates business logic

Application.Server:
- MUST reference Application and Domain
- MUST NOT reference Application.Client
- MAY reference the same core abstractions and DI helpers as Application
- Contains server-specific logic

Application.Client:
- MUST reference Application and Domain
- MUST NOT reference Application.Server
- MAY reference the same core abstractions and DI helpers as Application
- Contains client-specific logic

Application.Server and Application.Client are parallel branches, not hierarchical.

Domain.Server:
- MUST reference Domain only
- MUST NOT reference Domain.Client
- Contains server-specific domain types (entities, value objects, enums)
- Used for Blazor Server or API-only scenarios

Domain.Client:
- MUST reference Domain only
- MUST NOT reference Domain.Server
- Contains client-specific domain types
- Used for Blazor WebAssembly scenarios

Domain.Server and Domain.Client are parallel branches for platform-specific domain logic.

Infrastructure:
- MUST reference Application and Domain
- MUST NOT reference Presentation
- Implements repository and UnitOfWork interfaces defined in Domain
- Implements external service interfaces defined in Application

Presentation:
- MUST reference Application and Domain
- MUST NOT reference Infrastructure except in Program.cs
- Drives the application through ports

---

## Layer Folder Structures

### Domain Layer

`Interfaces/` in Domain contains marker interfaces (`IDomainEvent`, `IAggregateRoot`) and persistence contracts (repositories, UnitOfWork).

```
Domain/
+-- Domain.cs                               <- ApplicationDependency
+-- Serialization/
|   +-- DomainJsonContext.cs
|   +-- Converters/
+-- Shared/
|   +-- Interfaces/                         <- Base contracts
|   |   +-- IAggregateRoot.cs               <- Marker interface
|   |   +-- IDomainEvent.cs                 <- Marker interface
|   |   +-- IEntity.cs                      <- Marker interface
|   |   +-- IUnitOfWork.cs                  <- Base UoW contract
|   +-- Models/
|   +-- Constants/                          <- Shared constants (EmptyCollections, etc.)
|   +-- Exceptions/                         <- Base exceptions (DomainException, etc.)
|   +-- Extensions/
|       +-- SharedServiceCollectionExtensions.cs
+-- {Feature}/
    +-- Entities/
    +-- ValueObjects/
    +-- Models/                             <- Non-entity domain types (plain classes, records)
    +-- Enums/
    +-- Events/
    +-- Interfaces/                         <- Domain contracts implemented by Infrastructure
    |   +-- I{Feature}Repository.cs         <- Repository interface
    |   +-- I{Feature}UnitOfWork.cs         <- Feature UoW interface
    +-- Constants/
    +-- Services/                           <- Domain services (pure logic)
    +-- Exceptions/
    +-- Extensions/
        +-- {Feature}ServiceCollectionExtensions.cs
```

### Application Layer

```
Application/
+-- Application.cs                          <- ApplicationDependency
+-- Serialization/
|   +-- ApplicationJsonContext.cs
+-- Shared/
|   +-- Interfaces/                         <- Internal abstractions
|   |   +-- Inbound/                        <- Incoming ports
|   |   +-- Outbound/                       <- Outgoing ports
|   +-- Models/
|   +-- Primitives/                         <- Instantiable utility classes
|   +-- Services/
|   +-- Extensions/                         <- Static extension methods
|       +-- SharedServiceCollectionExtensions.cs
+-- {Feature}/
    +-- Interfaces/
    |   +-- Inbound/
    |   +-- Outbound/
    +-- Services/
    +-- Models/
    +-- Workers/                            <- Background entry points
    +-- Validators/
    +-- EventHandlers/
    +-- Extensions/
        +-- {Feature}ServiceCollectionExtensions.cs
```

### Application.Server Layer

```
Application.Server/
+-- ServerApplication.cs                    <- ApplicationDependency
+-- Serialization/
|   +-- ApplicationServerJsonContext.cs
+-- Shared/
|   +-- Interfaces/
|   |   +-- Inbound/
|   |   +-- Outbound/
+-- {Feature}/
    +-- Interfaces/
    |   +-- Inbound/
    |   +-- Outbound/
    +-- Services/
    +-- Models/
    +-- Workers/                            <- Server-only workers
    +-- EventHandlers/
    +-- Extensions/
        +-- {Feature}ServiceCollectionExtensions.cs
```

### Application.Client Layer

```
Application.Client/
+-- ClientApplication.cs                    <- ApplicationDependency
+-- Serialization/
|   +-- ApplicationClientJsonContext.cs
+-- Shared/
|   +-- Interfaces/
|   |   +-- Inbound/
|   |   +-- Outbound/
+-- {Feature}/
    +-- Interfaces/
    |   +-- Inbound/
    |   +-- Outbound/
    +-- Services/
    +-- Models/
    +-- EventHandlers/
    +-- Extensions/
        +-- {Feature}ServiceCollectionExtensions.cs
```

### Infrastructure Layer

```
Infrastructure.{Provider}/
+-- {Name}Infrastructure.cs                 <- ApplicationDependency
+-- Serialization/
|   +-- {Name}JsonContext.cs
+-- Adapters/                               <- Outgoing adapters
+-- Services/
+-- Repositories/
+-- Configurations/
+-- Extensions/
    +-- {Name}ServiceCollectionExtensions.cs
+-- Models/

Infrastructure.{Provider}.{Feature}/
+-- Adapters/
+-- Repositories/
+-- Configurations/
+-- Extensions/
```

### Presentation Layer

```
Presentation/
+-- Commands/
|   +-- BaseCommand.cs
+-- Contracts/
|   +-- {Feature}/
|       +-- Requests/
|       +-- Responses/
|       +-- Hubs/
+-- Shared/
    +-- Components/
    +-- Extensions/

Presentation.WebApp/
+-- Commands/
|   +-- BaseWebAppCommand.cs
+-- Components/
|   +-- Layout/
|   +-- Pages/
|   +-- Shared/
+-- Services/
+-- Models/

Presentation.WebApp.Server/
+-- Program.cs                              <- Composition root
+-- Commands/
|   +-- MainCommand.cs
+-- Controllers/                            <- Incoming adapters
+-- Workers/                                <- Worker hosts
+-- Extensions/

Presentation.WebApp.Client/
+-- Program.cs                              <- Composition root
+-- Commands/
|   +-- MainCommand.cs
+-- Components/                             <- Incoming adapters (Blazor)
|   +-- Layout/
|   +-- Pages/
|   +-- Shared/
+-- Services/
+-- Models/

Presentation.WebApi/
+-- Program.cs                              <- Composition root
+-- Commands/
|   +-- MainCommand.cs
+-- Controllers/V{n}/                       <- Incoming adapters
+-- Models/
|   +-- Requests/
|   +-- Responses/
+-- Middleware/
+-- Filters/

Presentation.Cli/
+-- Program.cs                              <- Composition root
+-- Commands/
|   +-- MainCommand.cs
+-- Services/
```

### Testing Layer

```
tests/
+-- Domain.UnitTests/
|   +-- {Feature}/
|       +-- Entities/
|       +-- Services/
|       +-- ValueObjects/
+-- Application.UnitTests/
|   +-- {Feature}/
|       +-- Services/
|       +-- EventHandlers/
+-- Application.IntegrationTests/
|   +-- {Feature}/
+-- Presentation.*.FunctionalTests/
    +-- {Feature}/
```

Test project rules:
- MUST mirror `src/` folder structure in `tests/` sibling folder
- MUST be named `{Layer}.{TestType}Tests` (e.g., `Domain.UnitTests`, `Application.IntegrationTests`)
- MUST use same namespace structure as source projects
- MUST place shared test helpers in `TestHelpers/` or base test class
- MUST use mocks for Infrastructure dependencies in unit tests
- MAY use real Infrastructure in integration tests

Source ignorance of tests:
- `src/` MUST be completely ignorant of `tests/`
- `src/` MUST NOT contain code that exists solely for testing purposes
- Tests MUST exercise `src/` code as-is, constructing their own test data using public APIs
- Tests MAY build custom permission trees or domain objects using public DSL utilities

| Test Type | Project Name | Tests Against |
|-----------|--------------|---------------|
| Unit | `Domain.UnitTests` | Entities, value objects, domain services |
| Unit | `Application.UnitTests` | Application services (mocked dependencies) |
| Integration | `Application.IntegrationTests` | Application + Infrastructure |
| Functional | `Presentation.*.FunctionalTests` | Full HTTP/UI stack |

---

## Interface Folders

Interfaces are organized into three folder categories with distinct access rules.

### Interfaces/Inbound/ (Incoming Ports)

Interfaces/Inbound:
- Define what the application offers to the outside world
- Are implemented by Application* services
- MAY be called by any layer
- Examples: `IOrderService`, `IIdentityService`, `IHelloWorldService`

### Interfaces/Outbound/ (Outgoing Ports)

Interfaces/Outbound:
- Define what the application needs from the outside world
- Are implemented by Infrastructure* adapters
- MAY be called by Application* and Infrastructure* only
- MUST NOT be called by Presentation
- Examples: `IEmailSender`, `ITokenProvider`, `IDateTimeProvider`

### Interfaces/ (Internal)

Interfaces (without subfolder):
- Are internal abstractions within Application layer
- Are implemented by Application* services
- MAY be called by Application* and Infrastructure* only
- MUST NOT be called by Presentation
- Examples: `IDomainEventDispatcher`, `IDomainEventHandler`, `IOfflineSyncManager`

### Interface Access Rules

| Interface Location | Who Can Use | Who Implements |
|--------------------|-------------|----------------|
| `Domain/{Feature}/Interfaces/` | Application*, Infrastructure* | Infrastructure* |
| `Domain/Shared/Interfaces/` | Application*, Infrastructure* | Infrastructure* (except markers) |
| `Application/Interfaces/Inbound/` | Any layer | Application* |
| `Application/Interfaces/Outbound/` | Application*, Infrastructure* | Infrastructure* |
| `Application/Interfaces/` | Application*, Infrastructure* | Application* |

Application* includes: `Application`, `Application.Server`, `Application.Client`
Infrastructure* includes: `Infrastructure.*` (all Infrastructure projects)

### Interface Placement

- Base `IUnitOfWork` MUST be in `Domain/Shared/Interfaces/`
- Feature UnitOfWork interfaces MUST be in `Domain/{Feature}/Interfaces/`
- Feature repository interfaces MUST be in `Domain/{Feature}/Interfaces/`
- Shared internal interfaces MUST be in `Application/Shared/Interfaces/`
- Shared Interfaces/Inbound MUST be in `Application/Shared/Interfaces/Inbound/`
- Shared Interfaces/Outbound MUST be in `Application/Shared/Interfaces/Outbound/`
- Server shared interfaces MUST be in `Application.Server/Shared/Interfaces/`
- Client shared interfaces MUST be in `Application.Client/Shared/Interfaces/`
- Feature internal interfaces MUST be in `Application/{Feature}/Interfaces/`
- Feature Interfaces/Inbound MUST be in `Application/{Feature}/Interfaces/Inbound/`
- Feature Interfaces/Outbound MUST be in `Application/{Feature}/Interfaces/Outbound/`

When Presentation needs functionality that uses internal interfaces:
- Create an Interfaces/Inbound service that uses the internal interface
- Presentation calls the Interfaces/Inbound service
- Presentation never touches the internal interface directly

---

## Adapters

Incoming adapters:
- Drive the application
- Live in Presentation layer
- Call Interfaces/Inbound
- Examples: Controllers, Blazor components, Presentation workers

Outgoing adapters:
- Are driven by the application
- Live in Infrastructure layer
- Implement Domain interfaces (repositories, UnitOfWork)
- Implement Application Interfaces/Outbound (external services)
- Examples: Repositories, HTTP clients, message bus clients

---

## Entry Points vs Services

Entry points:
- Are the initiators of application logic
- Create scopes and resolve services
- Are not injectable
- Do not have DI lifetime in the traditional sense

Entry points include:
- Controllers - HTTP request entry point
- Application Workers - Background task entry point
- Blazor Components - UI interaction entry point
- Queue Consumers - Message entry point
- Event Listeners - Event entry point

Services:
- Are injectable units of logic
- Are registered with DI lifetime
- Are resolved by entry points or other services

Services include:
- Domain Services - Pure business logic (Singleton)
- Application Services - Orchestration with I/O (Scoped)
- Repositories - Data access (Scoped)

---

## Service Categories

### Domain Services

Domain services:
- Contain pure business logic
- MUST NOT perform I/O
- MUST NOT depend on repositories, HTTP clients, or external services
- Receive all required data as method parameters
- Return calculated results
- MUST be registered as Singleton
- Are stateless calculators
- Produce same output for same input

Examples: `OrderPricingService`, `RiskAssessmentService`, `PasswordStrengthService`.

### Application Services

Application services:
- Orchestrate business operations
- Implement Interfaces/Inbound interfaces
- Call Domain services for business logic
- Call Interfaces/Outbound for I/O operations
- Coordinate transactions through IUnitOfWork
- Are typically Scoped when depending on DbContext, ICurrentUser, or IUnitOfWork
- MAY be Singleton if stateless and not depending on Scoped services

Examples: `OrderService`, `IdentityService`, `MarketStreamerService`.

---

## Unit of Work Pattern

Unit of Work coordinates persistence and domain event dispatch across repositories within an atomicity boundary.

### Base Interface

`IUnitOfWork` is a base interface in `Domain/Shared/Interfaces/`:
- Defines the `CommitAsync()` contract
- Is NOT directly implemented by Infrastructure
- Is inherited by feature-specific UnitOfWork interfaces

### Feature-Specific Interfaces

Each feature defines its own UnitOfWork interface in `Domain/{Feature}/Interfaces/`:
- Inherits from base `IUnitOfWork`
- Declares the atomicity boundary for that feature
- Is implemented by Infrastructure adapters

```csharp
// Domain/Shared/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

// Domain/Trading/Interfaces/ITradingUnitOfWork.cs
public interface ITradingUnitOfWork : IUnitOfWork;

// Domain/Identity/Interfaces/IIdentityUnitOfWork.cs
public interface IIdentityUnitOfWork : IUnitOfWork;
```

### UnitOfWork Rules

- Base `IUnitOfWork` MUST be in `Domain/Shared/Interfaces/`
- Feature UnitOfWork interfaces MUST be in `Domain/{Feature}/Interfaces/`
- Feature UnitOfWork interfaces MUST inherit from `IUnitOfWork`
- Infrastructure MUST implement feature-specific interfaces, NOT base `IUnitOfWork`
- Services MUST inject feature-specific interfaces, NOT base `IUnitOfWork`
- Each UnitOfWork defines one atomicity boundary
- A feature MAY have multiple UnitOfWork interfaces
- Repositories sharing a UnitOfWork are atomic together

### Infrastructure Implementation

Infrastructure decides how to implement each feature UnitOfWork:

| Domain Declares | Infrastructure May Implement |
|-----------------|------------------------------|
| `ITradingUnitOfWork` | Same DbContext as Identity |
| `ITradingUnitOfWork` | Separate TradingDbContext |
| `ITradingUnitOfWork` | Redis + Kafka |
| `IIdentityUnitOfWork` | PostgreSQL |
| `IInventoryUnitOfWork` | MongoDB |

Domain declares atomicity intent. Infrastructure decides mechanism.

---

## Domain Base Models

Base models are in `Domain/Shared/Models/`.

| Base Class | Purpose |
|------------|---------|
| `Entity` | Identity with `Id` and `RevId` |
| `AggregateRoot` | Entity with domain events |
| `AuditableEntity` | Entity with `Created`/`LastModified` |
| `ValueObject` | Immutable equality-by-components |

---

## Domain Type Categories

Domain types are organized into three categories based on their characteristics.

| Category | Location | Characteristics | Examples |
|----------|----------|-----------------|----------|
| Entities | `Entities/` | Extends `Entity`/`AggregateRoot`, has identity, mutable state | `User`, `Order`, `Role` |
| ValueObjects | `ValueObjects/` | Extends `ValueObject`, immutable, equality by components | `Email`, `Money`, `UserId` |
| Models | `Models/` | Plain classes/records, no base class required, domain data structures | `ApiKey`, `LoginSession`, `Permission` |

When to use each:
- USE Entities for things with unique identity that are tracked over time
- USE ValueObjects for immutable concepts defined by their attributes
- USE Models for domain data structures that don't fit Entity or ValueObject patterns

---

## Entity Implementation

Entities use protected constructors for EF Core hydration and subclass inheritance, and static factory methods for creation.

```csharp
public class OrderEntity : AggregateRoot
{
    public string CustomerName { get; private set; }
    public decimal Total { get; private set; }

    protected OrderEntity(Guid id, string customerName, decimal total) : base(id)
    {
        CustomerName = customerName;
        Total = total;
    }

    public static OrderEntity Create(string customerName, decimal total)
    {
        var entity = new OrderEntity(
            Guid.NewGuid(),
            customerName ?? throw new ArgumentNullException(nameof(customerName)),
            total);
        entity.AddDomainEvent(new OrderCreatedEvent(entity.Id));
        return entity;
    }
}
```

Entity implementation rules:
- MUST extend `Entity`, `AggregateRoot`, or `AuditableEntity`
- MUST use `private set` on all properties
- MUST use `protected` constructor with all properties as parameters
- MUST only assign values in constructor
- MUST use static factory methods for business creation
- MUST raise domain events only in factory methods
- MUST validate parameters only in factory methods

---

## ValueObject Implementation

Value objects use protected constructors for subclass inheritance, and static factory methods for creation.

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    protected Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        return new Money(amount, currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

ValueObject implementation rules:
- MUST extend `ValueObject`
- MUST override `GetEqualityComponents()`
- MUST use read-only properties (`{ get; }`)
- MUST use `protected` constructor
- MUST only assign values in constructor
- MUST use static factory methods for business creation
- MUST validate parameters only in factory methods

---

## Domain Exceptions

Domain exceptions provide a hierarchy for domain-level error handling.

### Exception Hierarchy

```
Exception
  └── DomainException              <- Base for all domain errors
        ├── EntityNotFoundException   <- Entity lookup failures
        └── ValidationException       <- Validation failures
```

### Base Exception

`DomainException` in `Domain/Shared/Exceptions/`:
- Base class for all domain-specific exceptions
- Provides standard constructors (message, message+inner)
- Feature-specific exceptions SHOULD extend this

### Built-in Exceptions

| Exception | Purpose | Properties |
|-----------|---------|------------|
| `DomainException` | Base domain error | - |
| `EntityNotFoundException` | Entity not found | `EntityType`, `EntityIdentifier` |
| `ValidationException` | Validation failure | `PropertyName` |

### Exception Placement

- Base exceptions MUST be in `Domain/Shared/Exceptions/`
- Feature-specific exceptions MUST be in `Domain/{Feature}/Exceptions/`

---

## Domain Events

Domain events decouple side effects from domain operations.

### Event Flow

1. Entity raises event via `AddDomainEvent()`
2. SaveChanges triggers dispatch (post-commit)
3. Handlers execute in parallel (independent side effects)

### Domain Layer Types

Domain event types:
- `IDomainEvent` - Marker interface for all domain events
- `IAggregateRoot` - Interface for entities that can raise domain events
- `DomainEvent` - Base record for domain events
- `AggregateRoot` - Base class with `AddDomainEvent()` method

Domain event locations:
- `Domain/Shared/Interfaces/IDomainEvent.cs`
- `Domain/Shared/Interfaces/IAggregateRoot.cs`
- `Domain/Shared/Models/DomainEvent.cs`
- `Domain/Shared/Models/Entity.cs`
- `Domain/{Feature}/Events/{Entity}{Action}Event.cs`

### Application Layer Types

Application event types:
- `IDomainEventHandler` - Interface for event handlers
- `IDomainEventDispatcher` - Interface for dispatching events
- `DomainEventHandler<T>` - Base class for typed handlers
- `DomainEventDispatcher` - Default dispatcher implementation

Application event locations:
- `Application/Shared/Interfaces/IDomainEventHandler.cs`
- `Application/Shared/Interfaces/IDomainEventDispatcher.cs`
- `Application/Shared/Models/DomainEventHandler.cs`
- `Application/Shared/Services/DomainEventDispatcher.cs`
- `Application/{Feature}/EventHandlers/{Action}Handler.cs`

### Infrastructure Layer Types

Infrastructure event types:
- `DomainEventInterceptor` - EF Core interceptor for post-commit dispatch

Infrastructure event locations:
- `Infrastructure.EFCore/Adapters/DomainEventInterceptor.cs`

### Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Event | `{Entity}{Action}Event` | `UserCreatedEvent`, `OrderPlacedEvent` |
| Handler | `{Action}Handler` | `SendWelcomeEmailHandler`, `LogGreetingHandler` |

### Handler Rules

Domain event handlers:
- MUST extend `DomainEventHandler<TEvent>`
- MUST be registered as `IDomainEventHandler` in DI
- MUST be independent (no order dependencies)
- MAY execute in parallel with other handlers
- MUST handle their own exceptions

### Dispatch Rules

Domain event dispatch:
- MUST occur after successful commit (post-commit)
- MUST dispatch all events from all modified aggregates
- MUST clear events from entities after dispatch
- MUST aggregate exceptions from parallel handlers

---

## Application Workers

Application workers:
- Are `BackgroundService` implementations for business-related background tasks
- Are **initiators** - they ACT by calling services, they do not get called
- Own their background loop and decide WHEN to run (scheduling, intervals, event triggers)
- Call Domain repositories and Application services for HOW to execute
- Are internal classes
- Are not injectable (services do not call workers, workers call services)
- Create scopes and resolve services within those scopes
- Are registered as hosted services via `AddHostedService<T>()`

Worker locations by scope:
- `Application/{Feature}/Workers/` - Workers that run on all platforms (server and client)
- `Application.Server/{Feature}/Workers/` - Server-only workers
- `Application.Client/{Feature}/Workers/` - Client-only workers

Examples: `AnonymousUserCleanupWorker`, `ApiKeyCleanupWorker`, `OrderExpirationWorker`, `MarketDataSyncWorker`.

---

## Presentation Workers

Presentation workers:
- Are rare exceptions for UI-only background tasks that cannot exist in Application layer
- Are `BackgroundService` implementations for pure UI concerns
- Handle presentation concerns only (not business logic)
- Live in `Presentation.*/Workers/`
- Are internal classes
- PREFER Application workers for all business-related background tasks

Examples: Toast notification timeout, UI animation loops, browser-only cleanup.

---

## Composition Root

Infrastructure wiring occurs only in `Program.cs` of executable projects.

```
Presentation.WebApp.Server/Program.cs
Presentation.WebApp.Client/Program.cs
Presentation.WebApi/Program.cs
Presentation.Cli/Program.cs
```

Controllers, components, services, and commands MUST NOT import Infrastructure namespaces.

---

## Abstraction Requirements

- Application services MUST depend on interfaces, not concrete types
- Infrastructure MUST implement Application interfaces
- Presentation MUST inject interfaces via DI

---

## Service Accessibility

Interfaces: `public`
Implementations: `internal`

```csharp
public interface ILocalStoreService { }
internal class IndexedDBLocalStoreService : ILocalStoreService { }
```

Domain types (entities, value objects, events) are `public` because they are shared across layers.
Infrastructure and Application service implementations are `internal` because they are resolved via DI.

---

## Dependency Injection Lifetime Rules

Singleton:
- MUST only inject other Singleton services
- MUST NOT inject Scoped services (captive dependency causes stale data, wrong user context, and concurrency bugs)
- When needing request-specific data, MUST pass data as method parameters

Scoped:
- MAY inject Singleton and Scoped services

Transient:
- MAY inject Singleton, Scoped, and Transient services

Entry points like Controllers and Application Workers do not follow these rules because they are not injectable. They create scopes and resolve services from those scopes.

### Lifetimes by Layer

Domain services MUST be Singleton. Domain services are stateless calculators with pure logic.

Application services MAY be Singleton, Scoped, or Transient depending on their dependencies and state requirements.

Infrastructure adapters are typically Scoped. Infrastructure adapters depend on DbContext for repositories. Infrastructure adapters depend on HttpClient for external APIs.

Infrastructure utilities MAY be Singleton. Examples: clock adapters, configuration providers, stateless factories.

---

## Infrastructure Separation

Providers (SQLite, Postgres) and Features (LocalStore, Identity) are independent.

- Provider projects MAY reference Application and Domain
- Provider projects MUST NOT reference Feature projects
- Feature projects MAY reference Application, Domain, and `IDbContextFactory<T>`
- Feature projects MUST NOT reference Provider projects
- Composition happens at Presentation layer only

Infrastructure naming convention is `Infrastructure.{FeatureCollection}.{SpecificCategory}`.

Feature collection projects:
- Implement repository interfaces for a specific bounded context
- Are ignorant of database providers
- Examples: `Infrastructure.EFCore.Server.Identity`, `Infrastructure.EFCore.Server.Trading`

Provider projects:
- Configure specific database or service providers
- Are ignorant of features
- Examples: `Infrastructure.EFCore.Sqlite`, `Infrastructure.EFCore.Postgres`

---

## Ignorance Principles

- Application and Domain MUST have no knowledge of EF Core, SQLite, or databases
- Application MUST have no knowledge of specific external services (Binance, etc.)
- Application MUST have no knowledge of Blazor, SignalR, or HTTP
- Domain MUST use single entity with Type enum, not separate entities per variant

---

## Naming Ignorance

Application layer classes reference only the abstractions they depend on. Infrastructure implementations are named after their specific technology.

- Application class depending on `ILocalStoreService` MUST be named `LocalStoreTokenStorage` (not `EFCoreTokenStorage`)
- Infrastructure class implementing `ILocalStoreService` MUST be named after implementation: `IndexedDBLocalStoreService`
- Infrastructure class implementing `IExchangeService` MUST be named after implementation: `BinanceExchangeService`

---

## Search Before Create

Before creating any type, utility, or pattern:

1. MUST search the codebase for existing types with similar purpose
2. MUST check these locations in order:
   - `Domain/Shared/` for domain primitives and interfaces
   - `Domain/{Feature}/ValueObjects/` for domain value types
   - `Domain/{Feature}/Services/` for domain logic
   - `Application/Shared/Models/` for shared DTOs and results
   - `Application/{Feature}/Models/` for feature-specific DTOs
   - `Application.Server/{Feature}/` for server-specific types
   - `Application.Client/{Feature}/` for client-specific types
3. If found: MUST use it or extend it
4. If not found: MUST create in the appropriate shared location

---

## One Concept, One Type, One Location

- If same type defined in multiple files, MUST keep one definition and delete others
- If same concept has different names, MUST consolidate to single canonical name
- If private type could be shared, MUST move to appropriate shared location
- If anonymous type used for known concept, MUST use the existing named type

---

## No Duplication

MUST extract when:
- Same logic appears 2+ times
- Same pattern emerges across files
- Same constant value used in multiple locations
- Same error handling repeated

Shared code locations by layer:

- Domain shared interfaces (base contracts) MUST go in `Domain/Shared/Interfaces/`
- Domain feature interfaces (repos, UoW) MUST go in `Domain/{Feature}/Interfaces/`
- Domain services MUST go in `Domain/{Feature}/Services/`
- Domain value objects MUST go in `Domain/{Feature}/ValueObjects/`
- Domain models (non-entity types) MUST go in `Domain/{Feature}/Models/`
- Domain logic MUST go in `Domain/Shared/` or `Domain/{Feature}/`
- Application service interfaces (Interfaces/Inbound) MUST go in `Application/{Feature}/Interfaces/Inbound/`
- Application infrastructure interfaces (Interfaces/Outbound) MUST go in `Application/{Feature}/Interfaces/Outbound/`
- Application services MUST go in `Application/{Feature}/Services/`
- Application models MUST go in `Application/{Feature}/Models/`
- Application shared internal interfaces MUST go in `Application/Shared/Interfaces/`
- Application shared Interfaces/Inbound MUST go in `Application/Shared/Interfaces/Inbound/`
- Application shared Interfaces/Outbound MUST go in `Application/Shared/Interfaces/Outbound/`
- Application feature internal interfaces MUST go in `Application/{Feature}/Interfaces/`
- Application utilities MUST go in `Application/Shared/` or `Application/{Feature}/Extensions/`
- Application workers MUST go in `Application/{Feature}/Workers/`, `Application.Server/{Feature}/Workers/`, or `Application.Client/{Feature}/Workers/`
- Application event handlers MUST go in `Application/{Feature}/EventHandlers/`, `Application.Server/{Feature}/EventHandlers/`, or `Application.Client/{Feature}/EventHandlers/`
- Infrastructure adapters MUST go in `Infrastructure.{Provider}.{Feature}/Adapters/`
- Infrastructure utilities MUST go in `Infrastructure.{Provider}/Extensions/`
- Presentation shared code MUST go in `Presentation/Shared/`
- Presentation contracts MUST go in `Presentation/Contracts/{Feature}/`
- Presentation workers MUST go in `Presentation.*/Workers/`
- Test helpers MUST go in base test class or `TestHelpers/`

---

## Constants Over Magic Values

Every literal value used more than once MUST be a named constant.

- Shared constants MUST be in `Domain/Shared/Constants/` (e.g., `EmptyCollections`)
- Domain constants MUST be in `Domain/{Feature}/Constants/`
- Application settings MUST be in Configuration or `Application/{Feature}/Constants/`
- Test values MUST be in test base class or constants file

### EmptyCollections Pattern

`EmptyCollections` in `Domain/Shared/Constants/` provides shared empty collection instances to avoid redundant allocations:

```csharp
public static class EmptyCollections
{
    public static IReadOnlyList<string> StringList { get; } = Array.Empty<string>();
    public static IReadOnlyList<Guid> GuidList { get; } = Array.Empty<Guid>();
    public static IReadOnlyDictionary<string, string> StringStringDictionary { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0, StringComparer.Ordinal));
}
```

---

## Consolidation Workflow

When duplicates are discovered:

1. MUST identify canonical location based on layer rules (prefer `Application/Models`, `Domain/Models`, or `Domain/ValueObjects`)
2. MUST keep the most complete definition
3. MUST update all references to use the shared type
4. MUST delete duplicate definitions
5. MUST verify build succeeds

---

## One Type Per File

Each file contains exactly one public type. File name matches the type name.

| Type Kind | File Name Format | Example |
|-----------|------------------|---------|
| Class | `{ClassName}.cs` | `UserService.cs` |
| Interface | `I{Name}.cs` | `IUserService.cs` |
| Record | `{RecordName}.cs` | `LoginRequest.cs` |
| Enum | `{EnumName}.cs` | `OrderStatus.cs` |

Exceptions:
- Private nested types within the containing type's file
- File-scoped types using `file` modifier

---

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes, Records, Structs | PascalCase | `UserService`, `LoginRequest` |
| Interfaces | `I` + PascalCase | `IUserService`, `ITokenProvider` |
| Methods | PascalCase | `GetUserAsync`, `ValidateToken` |
| Properties | PascalCase | `UserId`, `IsAuthenticated` |
| Private fields | `_camelCase` | `_userService`, `_logger` |
| Local variables | camelCase | `userId`, `tokenResult` |
| Constants | PascalCase | `DefaultTimeout`, `MaxRetries` |
| Parameters | camelCase | `userId`, `cancellationToken` |
| Async methods | Suffix `Async` | `GetUserAsync`, `SaveAsync` |

---

## Member Ordering Within Files

1. Constants
2. Static fields
3. Instance fields
4. Constructors
5. Properties
6. Public methods
7. Private methods

---

## Namespace Convention

Namespace mirrors folder path from `src/`.

```
src/Application/Identity/Services/UserService.cs
-> namespace Application.Identity.Services;
```

---

## Type Placement by Kind

Files MUST be placed in folders matching their type kind.

| Type Kind | Required Folder | Example |
|-----------|-----------------|---------|
| Interface (internal) | `Interfaces/` | `Interfaces/IDomainEventDispatcher.cs` |
| Interface (Inbound) | `Interfaces/Inbound/` | `Interfaces/Inbound/IOrderService.cs` |
| Interface (Outbound) | `Interfaces/Outbound/` | `Interfaces/Outbound/IEmailSender.cs` |
| Enum | `Enums/` | `Enums/OrderStatus.cs` |
| Record (DTO/Model) | `Models/` | `Models/LoginRequest.cs` |
| Service class | `Services/` | `Services/UserService.cs` |
| Adapter class | `Adapters/` | `Adapters/BinanceExchangeAdapter.cs` |
| Repository class | `Repositories/` | `Repositories/OrderRepository.cs` |
| Entity | `Entities/` | `Entities/User.cs` |
| Value object | `ValueObjects/` | `ValueObjects/Email.cs` |
| Exception | `Exceptions/` | `Exceptions/UserNotFoundException.cs` |
| Extension class | `Extensions/` | `Extensions/ServiceCollectionExtensions.cs` |
| Primitive class | `Primitives/` | `Primitives/GateKeeper.cs` |
| Utility class | `Utilities/` | `Utilities/NetworkUtils.cs` |
| Validator | `Validators/` | `Validators/LoginRequestValidator.cs` |
| Configuration | `Configurations/` | `Configurations/UserConfiguration.cs` |
| Constants class | `Constants/` | `Constants/ErrorMessages.cs` |
| Application Worker | `Workers/` | `Workers/TradeFiller.cs` |
| Presentation Worker Host | `Workers/` | `Workers/TradeFillerHost.cs` |
| Domain Event | `Events/` | `Events/OrderPlacedEvent.cs` |
| Event Handler | `EventHandlers/` | `EventHandlers/SendWelcomeEmailHandler.cs` |
| ApplicationDependency | Project root | `Application.cs` |
| ServiceCollectionExtensions | `Extensions/` | `Extensions/LocalStoreServiceCollectionExtensions.cs` |
| ConfigurationExtensions | `Extensions/` | `Extensions/EFCoreSqliteConfigurationExtensions.cs` |
| JsonContext | `Serialization/` | `Serialization/DomainJsonContext.cs` |
| JsonConverter | `Serialization/Converters/` | `Serialization/Converters/CamelCaseEnumConverter.cs` |
| Command | `Commands/` | `Commands/MainCommand.cs` |

Verification:
- Before creating a file, identify its type kind
- Place in the corresponding folder within the feature/component area
- If folder does not exist, create it

---

## Primitives, Utilities, and Extensions

Primitives, Utilities, and Extensions serve different purposes in shared utility code.

### Primitives

Primitives are instantiable utility classes that:
- Are created with `new` keyword
- Hold state or manage resources
- Provide reusable building blocks
- Live in `Primitives/` folder

Examples: `GateKeeper`, `AsyncManualResetEvent`, `AsyncReaderWriterLock`, `ValueKeeper`, `LazyValue`.

### Utilities

Utilities are static helper classes that:
- Are called directly without instantiation
- Do NOT use `this` parameter (not extension methods)
- Provide stateless helper functions
- Live in `Utilities/` folder

Examples: `NetworkUtils`, `RandomHelpers`, `TaskUtils`, `StringEncoder`, `SecureDataHelpers`.

### Extensions

Extensions are static extension methods that:
- Are called directly without instantiation
- Extend existing types via `this` parameter
- Provide stateless transformations on existing types
- Live in `Extensions/` folder

Examples: `ServiceCollectionExtensions`, `ConfigurationExtensions`, `StringExtensions`, `CancellationTokenExtensions`.

---

## FAQ / Edge Cases

**Q: Where do I put a service used by both Server and Client?**
A: `Application/{Feature}/Services/` - The base Application layer is shared by both Application.Server and Application.Client.

**Q: What if Infrastructure needs to call another Infrastructure adapter?**
A: Create an interface in `Application/{Feature}/Interfaces/Outbound/`. Infrastructure adapters should not call each other directly.

**Q: Where do constants shared across all layers go?**
A: `Domain/Shared/Constants/` - Domain is referenced by all layers.

**Q: Can a domain service call a repository?**
A: No. Domain services are pure logic with no I/O. Pass data as method parameters. Application services orchestrate repos and domain services.

**Q: Where do DTOs for external API responses go?**
A: `Infrastructure.{Provider}/Models/` - They are implementation details of the outbound adapter.

**Q: When should I use Application.Server vs Application?**
A: Use `Application.Server/` for logic that only runs on server (e.g., background jobs requiring server resources). Use `Application/` for shared logic.

**Q: Can Presentation call Interfaces/Outbound directly?**
A: No. Presentation calls Interfaces/Inbound only. Create a service in Application that wraps the outbound call if needed.

---

## Prohibited Patterns

- NEVER use `@inject DbContext` in components
- NEVER use `using Infrastructure.*` outside Program.cs
- NEVER use concrete types in Application constructor parameters
- NEVER use framework attributes (`[Key]`, `[JsonProperty]`) in Domain
- NEVER use static service locator patterns
- NEVER hardcode connection strings or URLs in Application
- NEVER use `if (type == X)` branching in Application layer
- NEVER implement Interfaces/Inbound in Infrastructure layer
- NEVER implement Interfaces/Outbound in Application layer
- NEVER call Interfaces/Outbound directly from Presentation layer
- NEVER call Infrastructure directly from Presentation layer (except DI registration)
- NEVER place business logic in Presentation workers
- NEVER place I/O operations in Domain services
- NEVER expose internal interfaces to Presentation layer
- NEVER inject Application Workers into other services
- NEVER copy-paste with minor variations
- NEVER use inline magic values (hardcoded strings, numbers, timeouts)
- NEVER duplicate validation logic
- NEVER repeat error handling patterns
- NEVER use anonymous types when named types exist
- NEVER place shared types in server-only or client-only projects when both need them
- NEVER have multiple public types in one file
- NEVER define DTOs inside controller files
- NEVER use nested public types
- NEVER leave empty placeholder/stub files after refactoring
- NEVER use file names that do not match the contained type
- NEVER place a type in a folder that does not match its kind
- NEVER create new base entity classes
- NEVER use `public set` on entity properties
- NEVER use `public` constructors on entities
- NEVER use `public` constructors on value objects
- NEVER add parameterless constructors to entities
- NEVER raise domain events in constructors
- NEVER validate in constructors
- NEVER add logic in constructors
- NEVER add `Id` to `ValueObject`
- NEVER use mutable properties in `ValueObject`

---

## ApplicationDependency

Every layer (except Presentation) MUST have an ApplicationDependency implementation at project root.

| Layer | Class Name | Location |
|-------|------------|----------|
| Domain | `Domain` | `Domain/Domain.cs` |
| Application | `Application` | `Application/Application.cs` |
| Application.Server | `ServerApplication` | `Application.Server/ServerApplication.cs` |
| Application.Client | `ClientApplication` | `Application.Client/ClientApplication.cs` |
| Infrastructure.* | `{Name}Infrastructure` | `Infrastructure.{Name}/{Name}Infrastructure.cs` |

ApplicationDependency rules:
- MUST extend `ApplicationDependency` directly
- MUST NOT extend another ApplicationDependency implementation
- MUST register services through ServiceCollectionExtensions
- MUST NOT register services directly in ApplicationDependency
- MUST call `base.Method()` in all lifecycle method overrides

Examples: `Domain`, `Application`, `ServerApplication`, `ClientApplication`, `EFCoreInfrastructure`, `EFCoreSqliteInfrastructure`, `IdentityInfrastructure`, `OpenTelemetryInfrastructure`.

---

## ApplicationDependency Lifecycle

Lifecycle methods execute in order:

1. `CommandPreparation(ApplicationBuilder)` - Before command argument parsing
2. `BuilderPreparation(ApplicationHostBuilder)` - After argument parsing, before host builder creation
3. `AddConfigurations(ApplicationHostBuilder, IConfiguration)` - After builder creation
4. `AddServices(ApplicationHostBuilder, IServiceCollection)` - After configuration
5. `AddMiddlewares(ApplicationHost, IHost)` - After DI registration
6. `AddMappings(ApplicationHost, IHost)` - After middleware
7. `RunPreparation(ApplicationHost)` - After mappings (parallel across layers)
8. `RunPreparationAsync(ApplicationHost, CancellationToken)` - After mappings (parallel across layers)

Method usage:
- `CommandPreparation` - Add custom type parsers
- `BuilderPreparation` - Prepare host builder
- `AddConfigurations` - Add configuration providers, bind IOptions
- `AddServices` - Register DI via ServiceCollectionExtensions only
- `AddMiddlewares` - Configure middleware pipeline
- `AddMappings` - Map endpoints, routes, SignalR hubs
- `RunPreparation` - Synchronous initialization/bootstrap
- `RunPreparationAsync` - Asynchronous initialization (database setup, etc.)

---

## ServiceCollectionExtensions

Every feature and Shared MUST have its own ServiceCollectionExtensions class.

ServiceCollectionExtensions rules:
- MUST be `internal static` class when called only by same-layer ApplicationDependency
- MAY be `public static` class when designed for cross-layer use (e.g., Presentation configuring options)
- MUST have extension methods for IServiceCollection
- MUST be named `{Feature}ServiceCollectionExtensions`
- MUST have methods named `Add{Feature}Services`
- MAY have `Add{Feature}Configurations` or `Add{Feature}Middlewares`

Shared ServiceCollectionExtensions:
- MUST be in `{Layer}/Shared/Extensions/SharedServiceCollectionExtensions.cs`
- MUST be `public static` class (accessible to Application.Server/Client)
- MUST have method named `AddSharedServices` (internal - called by ApplicationDependency)
- MAY have public utility methods for cross-layer use (e.g., `AddDomainEventHandler<T>`)

```csharp
namespace Application.Shared.Extensions;

public static class SharedServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return services;
    }

    public static IServiceCollection AddDomainEventHandler<THandler>(this IServiceCollection services)
        where THandler : class, IDomainEventHandler
    {
        services.AddScoped<IDomainEventHandler, THandler>();
        return services;
    }
}
```

---

## ConfigurationExtensions

ConfigurationExtensions enable cross-layer configuration sharing. Any layer can set or read values.

### ApplicationBuilderHelpers Library Methods

The `ApplicationBuilderHelpers` library provides base methods with `@ref:` reference chain support:

| Method | Purpose |
|--------|---------|
| `GetRefValue(key)` | Get value, follows `@ref:` chains, throws if not found |
| `GetRefValueOrDefault(key, default)` | Get value or default, follows `@ref:` chains |
| `TryGetRefValue(key, out value)` | Try-pattern version |
| `ContainsRefValue(key)` | Check if value exists |

Reference chain example:
```
# Direct value - returned as-is
DB_HOST = "localhost"
config.GetRefValue("DB_HOST") → "localhost"

# Reference value - follows the chain
DB_HOST = "@ref:PROD_DB_HOST"
PROD_DB_HOST = "db.example.com"
config.GetRefValue("DB_HOST") → "db.example.com"

# Chained references - follows until reaching a value
DB_HOST = "@ref:ENV_DB_HOST"
ENV_DB_HOST = "@ref:PROD_DB_HOST"
PROD_DB_HOST = "db.example.com"
config.GetRefValue("DB_HOST") → "db.example.com"
```

### Shared Type Helpers

`Application/Shared/Extensions/ConfigurationExtensions.cs` provides type-specific helpers:

| Method | Purpose |
|--------|---------|
| `GetBoolean(key)` | Parse with flexible formats (enabled/true/yes/1) |
| `GetBooleanOrDefault(key, default)` | Boolean with default |
| `SetBoolean(key, value)` | Store as "true"/"false" |

### Feature-Specific Extensions

Each feature defines its own ConfigurationExtensions for its settings.

ConfigurationExtensions provide a unified way to share settings across layers regardless of source. Whether a value comes from CLI arguments, environment variables, config files, or hardcoded defaults - consumers read it the same way via extension methods.

### When to Create ConfigurationExtensions

| Scenario | Create ConfigurationExtensions? |
|----------|--------------------------------|
| Setting needs to be shared across layers | Yes |
| Setting comes from any source (CLI, env var, config file, code) | Yes |

### Accessibility by Location

| Extension Location | Accessible By |
|-------------------|---------------|
| `Application/{Feature}/Extensions/` | All layers (Application, Infrastructure, Presentation) |
| `Application.Server/{Feature}/Extensions/` | Application.Server, server Infrastructure, server Presentation |
| `Application.Client/{Feature}/Extensions/` | Application.Client, client Infrastructure, client Presentation |

### Creation Steps

1. Create file at `Application/{Feature}/Extensions/{Feature}ConfigurationExtensions.cs`
2. Define key as `private const string` with `RUNTIME_` prefix
3. Add Get method using:
   - `GetRefValueOrDefault` - for optional settings with sensible defaults
   - `GetRefValue` - for required settings that throw if not configured
4. Add Set method storing value to configuration
5. Caller sets value from any source (CLI option, env var, hardcoded, etc.)
6. Any layer reads via Get method when needed

### ConfigurationExtensions Rules

- MUST be `public static` class with `extension(IConfiguration)` block (C# 13+ syntax)
- MAY use traditional `this IConfiguration` syntax for C# 12 compatibility
- MUST be named `{Feature}ConfigurationExtensions`
- MUST use private const string for key names
- MUST have property with getter and setter
- MUST use `GetRefValueOrDefault` for reference chain support

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

### Usage Example

```csharp
// Presentation/Commands/BaseCommand.cs - Setting the value
public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
{
    base.AddConfigurations(applicationBuilder, configuration);
    
    // Set from CLI option (or any source: env var, hardcoded, etc.)
    configuration.LoggerLevel = LogLevel;
}

// Presentation/Commands/BaseCommand.cs - Reading the value (fallback logging)
public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.SetMinimumLevel(applicationBuilder.Configuration.LoggerLevel);
        builder.AddConsole();
    });
}

// Infrastructure.OpenTelemetry - Reading the same value
public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
{
    var logLevel = configuration.LoggerLevel;  // Reads what Presentation set
    // Configure Serilog with this level...
}
```

### Configuration Flow

```
Presentation (BaseCommand.AddConfigurations)
    ↓
configuration.SetLoggerLevel(LogLevel)        ← Store setting
    ↓
Infrastructure (OpenTelemetryInfrastructure.AddConfigurations)
    ↓
configuration.GetLoggerLevel()                ← Read setting
```

This pattern allows:
- Presentation to set values from CLI options/environment variables
- Infrastructure to read values without knowing their source
- Either layer to be absent without breaking the other

---

## Serialization

Each layer MAY have a `Serialization/` folder for source-generated JSON contexts (Native AOT support).

JsonContext rules:
- MUST be `partial class` extending `JsonSerializerContext`
- MUST use `[JsonSourceGenerationOptions]` with `GenerationMode = Metadata`
- MUST use `[JsonSerializable(typeof(T))]` for each type to serialize
- MUST be named `{Layer}JsonContext`
- MUST be `public` if shared across layers (Domain, Application)
- MUST be `internal` if layer-internal only (Infrastructure)

| Layer | Class Name | Accessibility |
|-------|------------|---------------|
| Domain | `DomainJsonContext` | `public` |
| Application | `ApplicationJsonContext` | `public` |
| Application.Server | `ApplicationServerJsonContext` | `public` |
| Application.Client | `ApplicationClientJsonContext` | `public` |
| Infrastructure.{Name} | `{Name}JsonContext` | `internal` |

Converters:
- MUST be in `Serialization/Converters/` subfolder
- MUST be named `{Name}JsonConverter`
- MUST extend `JsonConverter<T>`

```csharp
namespace Domain.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Role))]
public partial class DomainJsonContext : JsonSerializerContext
{
}
```

---

## BuildConstantsGenerator

Presentation leaf projects MUST enable BuildConstantsGenerator for build constants and command wiring.

csproj configuration:

```xml
<PropertyGroup>
    <BaseCommandType>Presentation.WebApp.Commands.BaseWebAppCommand</BaseCommandType>
    <OutputType>Exe</OutputType>
</PropertyGroup>
```

| Property | Required | Description |
|----------|----------|-------------|
| `BaseCommandType` | Yes | Fully qualified name of intermediate base command. Setting this automatically enables `GenerateBuildConstants` |
| `OutputType` | Yes | Must be `Exe` for executable projects |

BaseCommandType by project family:

| Project Family | BaseCommandType |
|----------------|-----------------|
| WebApp.Server | `Presentation.WebApp.Commands.BaseWebAppCommand` |
| WebApp.Client | `Presentation.WebApp.Commands.BaseWebAppCommand` |
| WebApi | `Presentation.Commands.BaseCommand` or custom |
| Cli | `Presentation.Commands.BaseCommand` or custom |

Generated types in `Build` namespace:
- `Build.Constants` - Static build constants
- `Build.ApplicationConstants` - `IApplicationConstants` implementation
- `Build.BaseCommand<T>` - Extends `BaseCommandType` with `ApplicationConstants` wired

`IApplicationConstants` is registered in DI by `BaseCommand.AddServices()`. Inject anywhere via constructor or `@inject`.

---

## Command Hierarchy

```
Presentation.Commands.BaseCommand<T>               <- Manual (shared)
         ^
Presentation.WebApp.Commands.BaseWebAppCommand<T>  <- Manual (optional intermediate)
         ^
Build.BaseCommand<T>                               <- Generated per-project
         ^
MainCommand                                        <- Manual (leaf command)
```

Commands:
- MUST be internal classes
- MUST extend `Build.BaseCommand<TBuilder>`
- MUST have `[Command]` attribute with description
- MUST override `ApplicationBuilder()` to create appropriate builder type
- MAY override lifecycle methods for command-specific DI
- MAY have `[CommandOption]` for CLI options
- MAY have `[CommandArgument]` for positional arguments

| Command Type | Class Name | Attribute |
|--------------|------------|-----------|
| Main/Default | `MainCommand` | `[Command("Main subcommand.")]` |
| Sub-command | `{Name}Command` | `[Command("{name}", description: "...")]` |
| Nested | `{Parent}{Child}Command` | `[Command("{parent} {child}", description: "...")]` |

---

## Command Attributes

`[Command]` - Applied to classes to define command metadata.

| Property | Type | Description |
|----------|------|-------------|
| `Term` | `string?` | Command name (e.g., `"migrate"`, `"serve"`) |
| `Description` | `string?` | Help text for the command |

Constructors:
- `[Command("Description only")]` - Default/main command
- `[Command("name", description: "Description")]` - Named subcommand

`[CommandOption]` - Applied to properties to define CLI options (flags).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Term` | `string?` | - | Long option name (`--log-level`) |
| `ShortTerm` | `char?` | - | Short option name (`-l`) |
| `EnvironmentVariable` | `string?` | - | Env var to read from |
| `Required` | `bool` | `false` | Whether required |
| `Description` | `string?` | - | Help text |
| `FromAmong` | `object[]` | `[]` | Allowed values |
| `CaseSensitive` | `bool` | `false` | Case sensitivity for allowed values |

Required can be specified via:
- C# `required` keyword on property
- `Required = true` in attribute

`[CommandArgument]` - Applied to properties to define positional arguments.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string?` | - | Argument name |
| `Description` | `string?` | - | Help text |
| `Position` | `int` | `0` | Positional order |
| `Required` | `bool` | `false` | Whether required |
| `FromAmong` | `object[]` | `[]` | Allowed values |
| `CaseSensitive` | `bool` | `false` | Case sensitivity for allowed values |

```csharp
[CommandOption('l', "log-level", EnvironmentVariable = "LOG_LEVEL", Description = "Level of logs to show.")]
public LogLevel LogLevel { get; set; } = LogLevel.Information;

[CommandOption("credentials-override", EnvironmentVariable = "CREDENTIALS_OVERRIDE")]
public string? CredentialsOverrideBase64 { get; set; }

[CommandOption("api-key", Description = "API key for authentication.")]
public required string ApiKey { get; set; }
```

---

## Program.cs Structure

Program.cs MUST:
- Create ApplicationBuilder using `ApplicationBuilder.Create()`
- Add ApplicationDependency implementations via `.AddApplication<T>()`
- Add Commands via `.AddCommand<T>()`
- Call `.RunAsync(args)`

```csharp
return await ApplicationBuilder.Create()
    .AddApplication<Domain>()
    .AddApplication<Application>()
    .AddApplication<ServerApplication>()
    .AddApplication<IdentityInfrastructure>()
    .AddApplication<EFCoreInfrastructure>()
    .AddApplication<EFCoreSqliteInfrastructure>()
    .AddCommand<MainCommand>()
    .RunAsync(args);
```

---

## CLI Command Exit

CLI commands that perform one-shot operations MUST call `cancellationTokenSource.Cancel()` at the end of the `Run` method to signal completion and allow the application to exit properly.

```csharp
protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
{
    await base.Run(applicationHost, cancellationTokenSource);

    // Perform work...

    // Signal completion to exit the application
    cancellationTokenSource.Cancel();
}
```

One-shot commands:
- MUST call `cancellationTokenSource.Cancel()` after completing work
- MUST NOT leave the application running indefinitely

Long-running commands (servers, watchers):
- MUST NOT call `cancellationTokenSource.Cancel()` manually
- MUST allow external cancellation (Ctrl+C) to stop the application

---

## Prohibited Patterns (ApplicationBuilderHelpers)

- NEVER register services directly in ApplicationDependency
- NEVER extend another ApplicationDependency implementation
- NEVER skip `base.Method()` call in ApplicationDependency overrides
- NEVER use constructor injection in Commands
- NEVER call lifecycle methods out of order
- NEVER add ApplicationDependency without corresponding ServiceCollectionExtensions
- NEVER register DI from another layer (all layers converge in Program.cs)
- NEVER forget `.RunAsync(args)` in Program.cs
- NEVER forget `cancellationTokenSource.Cancel()` in one-shot CLI commands
