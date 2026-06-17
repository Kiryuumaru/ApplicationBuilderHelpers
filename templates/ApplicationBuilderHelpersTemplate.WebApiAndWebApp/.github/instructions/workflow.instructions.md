---
applyTo: '**'
---
# Workflow Rules

## Build Commands

### NUKE Build Commands

| Command | Description |
|---------|-------------|
| `.\build.ps1 init` | Generate `creds.json` |
| `.\build.ps1 clean` | Clean build artifacts (bin, obj, .vs) |
| `.\build.ps1 githubworkflow` | Generate GitHub Actions workflows |

### .NET Commands

| Command | Description |
|---------|-------------|
| `dotnet build` | Build solution |
| `dotnet test` | Run all tests |
| `dotnet test tests/Domain.UnitTests` | Run Domain tests |
| `dotnet test tests/Application.UnitTests` | Run Application tests |
| `dotnet run --project src/Presentation.WebApp` | Run web application |
| `dotnet run --project src/Presentation.Cli` | Run CLI application |

### Publishing

Run `dotnet build --no-incremental` before publishing to ensure CSS regeneration.

```powershell
dotnet build src/Presentation.WebApp.Server --no-incremental
dotnet publish src/Presentation.WebApp.Server -o publish
```

### Running Published Application

Use single combined command with absolute path:

```powershell
Push-Location "C:\path\to\publish"; & "C:\path\to\publish\sampleapp.exe" --urls "http://0.0.0.0:5000"
```

| Flag | Description |
|------|-------------|
| `--urls "http://0.0.0.0:5000"` | Listen on all interfaces, port 5000 |
| `--urls "http://localhost:5000"` | Listen on localhost only |

### Stop Running Instance

```powershell
Get-Process sampleapp -ErrorAction SilentlyContinue | Stop-Process -Force
```

### First-Time Setup

1. `.\build.ps1 init` - Generate credentials
2. `dotnet build` - Build solution

### Environment Configuration

Environments configured in `src/Domain/AppEnvironment/Constants/AppEnvironments.cs`:

| Environment | Tag | Short |
|-------------|-----|-------|
| Development | `prerelease` | `pre` |
| Production | `master` | `prod` |

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
