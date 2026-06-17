# Application Builder Helpers Template

A clean architecture application template for building .NET applications with authentication, authorization, and user management out of the box. Built with .NET 10 and Clean Architecture principles.

## 🚀 Quick Start

Run this command to create a new project from this template:

```powershell
C:\Windows\System32\WindowsPowerShell\v1.0\powershell -c "& ([ScriptBlock]::Create((irm https://raw.githubusercontent.com/Kiryuumaru/ApplicationBuilderHelpersTemplate/master/init.ps1)))"
```

## 📋 Overview

This template provides a solid foundation for building web applications with:

- **JWT Authentication** - Full auth flow with 2FA, passkeys (WebAuthn), and sessions
- **User Management** - Admin and self-service user operations
- **RBAC Authorization** - Role-based access control with fine-grained permissions
- **Clean Architecture** - Domain-driven design with proper layer separation
- **Comprehensive Testing** - Unit, integration, and functional test structure

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Documentation Index](docs/index.md) | Complete documentation index |
| [Authentication](docs/features/authentication.md) | JWT auth, 2FA, passkeys, sessions |
| [Anonymous Auth](docs/features/anonymous-authentication.md) | Guest mode and account linking |
| [User Management](docs/features/user-management.md) | User CRUD, roles, permissions |
| [Authorization Architecture](docs/architecture/authorization-architecture.md) | Permission system, RBAC |
| [Test Architecture](docs/architecture/test-architecture.md) | Test setup and conventions |

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              PRESENTATION                               │
│                   WebApi (REST + SignalR) │ WebApp (Blazor)             │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                          INFRASTRUCTURE                           │  │
│  │ EFCore │ EFCore.Identity │ EFCore.LocalStore │ Passkeys │ Serilog │  │
│  │  ┌─────────────────────────────────────────────────────────────┐  │  │
│  │  │                        APPLICATION                          │  │  │
│  │  │       Authorization │ Identity │ Configuration │ Logger     │  │  │
│  │  │  ┌───────────────────────────────────────────────────────┐  │  │  │
│  │  │  │                       DOMAIN                          │  │  │  │
│  │  │  │       Identity │ Authorization │ AppEnvironment       │  │  │  │
│  │  │  │                   No Dependencies                     │  │  │  │
│  │  │  └───────────────────────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘

Dependencies flow inward: Outer layers depend on inner layers, never reverse.
```

| Layer | Description |
|-------|-------------|
| **Domain** | Core entities, value objects, and business rules. Has no external dependencies. |
| **Application** | Business logic, services, interfaces. Depends only on Domain. Persistence/infrastructure ignorant. |
| **Infrastructure** | EF Core, Identity, Passkeys, Logging. Implements Application interfaces. |
| **Presentation** | REST API (WebApi), Blazor UI (WebApp). Composes all layers via DI. |

## ✨ Features

### Authentication
- **JWT Tokens** - Access tokens (60 min) + refresh tokens (7 days)
- **Two-Factor Auth (2FA)** - TOTP-based with recovery codes
- **Passkeys (WebAuthn)** - Passwordless authentication
- **Session Management** - Track and revoke active sessions
- **Anonymous Auth** - Guest mode with account upgrade path
- **OAuth Support** - External provider integration ready

### User Management
- **User CRUD** - Create, read, update, delete users
- **Role Assignment** - Assign roles with scope templates
- **Self-Service** - Profile management, password changes
- **Admin Operations** - Manage any user's data

### Security
- **RBAC** - Role-based access control
- **Permission Scopes** - Fine-grained endpoint authorization
- **`[FromJwt]` Pattern** - Automatic JWT claim binding

## 📁 Project Structure

```
├── build/                                  # NUKE build automation
├── docs/                                   # Documentation
│   ├── index.md                            # Documentation index
│   ├── api/                                # API documentation
│   ├── architecture/                       # Architecture docs
│   └── features/                           # Feature documentation
├── src/
│   ├── Domain/                             # Entities, ValueObjects, Business Rules
│   ├── Domain.SourceGenerators/            # Roslyn analyzers (authorization identifiers + build constants)
│   ├── Application/                        # Services, Interfaces
│   ├── Infrastructure.EFCore/              # Base EF Core DbContext
│   ├── Infrastructure.EFCore.Identity/     # Identity stores
│   ├── Infrastructure.EFCore.LocalStore/   # Key-value storage
│   ├── Infrastructure.Passkeys/            # WebAuthn/Passkey support
│   ├── Infrastructure.Serilog.Logger/      # Structured logging
│   ├── Presentation.WebApp/                # REST API controllers
│   └── Presentation.WebApp/                # Blazor Server UI
├── tests/
│   ├── Domain.UnitTests/                   # Domain logic tests
│   ├── Application.UnitTests/              # Application service tests
│   ├── Application.IntegrationTests/       # Integration tests
│   └── Presentation.WebApp.FunctionalTests/# API functional tests
└── ApplicationBuilderHelpersTemplate.sln
```

## ⚙️ Environment Configuration

Environments are configured in `src/Domain/AppEnvironment/Constants/AppEnvironments.cs`. This is the **single source of truth** for all environment-related configuration, following the same pattern as `Roles.cs` and `Permissions.cs`:

```csharp
public static class AppEnvironments
{
    public static AppEnvironment Development { get; } = new()
    {
        Tag = "prerelease",
        Environment = "Development",
        EnvironmentShort = "pre"
    };

    public static AppEnvironment Production { get; } = new()
    {
        Tag = "master",
        Environment = "Production",
        EnvironmentShort = "prod"
    };

    public static AppEnvironment[] AllValues { get; } = [Development, Production];
}
```

| Property | Description |
|----------|-------------|
| `Tag` | Git branch tag (e.g., `prerelease`, `master`) |
| `Environment` | Environment name, also used as property name |
| `EnvironmentShort` | Short identifier (e.g., `pre`, `prod`) |

> **Note:** The **last environment** in `AllValues` is treated as the main/production branch.

Running `.\build.ps1 init` generates `creds.json` with JWT secrets per environment (only if not exists).

## 🔐 Credentials (`creds.json`)

The `creds.json` file contains environment-specific credentials and is **not committed to the repository**. 

Generated with **secure 64-character alphanumeric secrets** per environment:

```json
{
  "prerelease": {
    "jwt": {
      "secret": "<auto-generated>",
      "issuer": "ApplicationBuilderHelpers",
      "audience": "ApplicationBuilderHelpers"
    }
  },
  "master": { ... }
}
```

The file will not be overwritten if it already exists.

## 🛠️ Build & Run

### Prerequisites
- .NET 10 SDK
- PowerShell Core (for build scripts)

### Commands
```powershell
.\build.ps1 init            # Generate creds.json (if not exists)
.\build.ps1 clean           # Clean build artifacts
.\build.ps1 githubworkflow  # Generate GitHub Actions workflow

dotnet build                # Build the solution
dotnet test                 # Run all tests

dotnet run --project src/Presentation.WebApp   # Run REST API
dotnet run --project src/Presentation.WebApp   # Run Blazor web app
```

## 🧪 Testing

Test projects are organized by layer:

| Project | Description |
|---------|-------------|
| Domain.UnitTests | Pure domain logic tests |
| Application.UnitTests | Application service tests |
| Application.IntegrationTests | Tests with real infrastructure |
| Presentation.WebApp.FunctionalTests | Full API endpoint tests |

```powershell
dotnet test                                          # Run all tests
dotnet test tests/Domain.UnitTests                   # Run domain tests
dotnet test tests/Presentation.WebApp.FunctionalTests # Run API tests
```

## 🔧 Customization

### Adding New Features
1. Define entities in `Domain/`
2. Create interfaces and services in `Application/`
3. Implement infrastructure in `Infrastructure.*/`
4. Add controllers in `Presentation.WebApp/`

### Switching Database Provider
Replace SQLite with PostgreSQL, SQL Server, etc. by creating a new Infrastructure provider project that implements the same interfaces.

### Adding External Integrations
Create a new `Infrastructure.{Provider}/` project that implements Application interfaces.

## 📜 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
