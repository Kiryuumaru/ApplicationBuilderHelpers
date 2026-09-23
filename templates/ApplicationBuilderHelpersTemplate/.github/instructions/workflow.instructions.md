---
applyTo: '**'
---
# Workflow Rules

## Build Commands

### NUKE Build Commands

| Command | Description |
|---------|-------------|
| `.\.build.ps1 init` | Generate `embedded-config.json` |
| `.\build.ps1 clean` | Clean build artifacts (bin, obj, .vs) |
| `.\build.ps1 githubworkflow` | Generate GitHub Actions workflows |

### .NET Commands

| Command | Description |
|---------|-------------|
| `dotnet build` | Build solution |
| `dotnet test` | Run all tests |
| `dotnet test tests/Domain.UnitTests` | Run Domain tests |
| `dotnet test tests/Application.UnitTests` | Run Application tests |
| `dotnet run --project src/Presentation.Cli` | Run CLI application |

### Publishing

```powershell
dotnet build src/Presentation.Cli --no-incremental
dotnet publish src/Presentation.Cli -o publish
```

### Running Published Application

The published binary name follows `AssemblyName` (defaults to `sampleapp`; renames automatically with it: substitute `<YourApp>` with your actual name):

```powershell
./publish/<YourApp>
```

### Stop Running Instance

```powershell
Get-Process <YourApp> -ErrorAction SilentlyContinue | Stop-Process -Force
```

### First-Time Setup

1. `.\build.ps1 init` - Generate embedded config
2. `dotnet build` - Build solution

### Environment Configuration

Environments configured in `src/Domain/AppEnvironment/Constants/AppEnvironments.cs`:

| Environment | Tag | Short |
|-------------|-----|-------|
| Development | `prerelease` | `pre` |
| Production | `master` | `prod` |

---

## Pre-Session Data Flow Mapping

Map the data flow before planning, coding, or implementation. Guessing creates technical debt.

### Why

Guessing creates technical debt: duplicate entities, leaked state, broken flows.

### What to Map

For every session, start by producing a brief data flow outline covering:

1. **Main entities**: What domain objects are involved? (e.g., `OrderEntity`, `PaymentEntity`)
2. **Data sources**: Where does data come from? (HTTP request, CLI input, background worker, external API, database)
3. **Data destinations**: Where does data go? (Database table, response body, notification, cache, message queue)
4. **What changes**: What state transitions happen? (e.g., `OrderCreated → PaymentProcessed → OrderCompleted`)

### How to Produce It

- Read the relevant architecture docs first (`architecture.instructions.md`)
- Scan the existing codebase for related entities, services, and repositories
- Write a **short** outline (5-10 lines max): not a giant architecture doc
- Paste it as the first output of the session

Example:

    Data Flow: User creates order
    1. Entities: OrderEntity, OrderItemEntity
    2. Source: HTTP POST /api/orders (Presentation.WebApi)
    3. Flow: OrderController → IOrderService.CreateAsync → OrderEntity.Create → IOrderRepository.AddAsync → DbContext.SaveChangesAsync
    4. Destination: Orders table (EF Core), OrderCreatedEvent dispatched
    5. Changes: OrderEntity state: Created → PendingPayment

### When to Skip

NEVER skip this step. It applies to every session regardless of scope: bug fix, new feature, refactoring, documentation update. The map may be tiny for small changes, but it grounds the AI.

---

## Pre-Commit Verification

Before every commit:

| Check | Command | Required Result |
|-------|---------|-----------------|
| Build | `dotnet build` | 0 warnings, 0 errors |
| Tests | `dotnet test` | 100% pass |

### Manual Review Checklist

- MUST follow architecture rules in `architecture.instructions.md`
- MUST follow code quality rules in `code-quality.instructions.md`
- MUST verify proper layer dependency direction per architecture rules
- MUST verify correct file placement per file structure rules
- MUST update documentation per `documentation.instructions.md`
