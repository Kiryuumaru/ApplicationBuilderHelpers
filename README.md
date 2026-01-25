# ApplicationBuilderHelpers

A powerful .NET library for building command-line applications with a fluent API, dependency injection, and modular architecture. ApplicationBuilderHelpers simplifies the creation of CLI tools by providing a structured approach to command handling, configuration management, and application composition.

## Features

- 🎯 **Command-based Architecture** - Build CLI applications using command patterns with automatic argument parsing
- 🔧 **Fluent Builder API** - Intuitive application setup using method chaining
- 💉 **Dependency Injection** - Full support for Microsoft.Extensions.DependencyInjection
- 🏗️ **Modular Application Structure** - Compose applications from reusable components
- ⚙️ **Configuration Management** - Seamless integration with .NET configuration system
- 🎨 **Attribute-based Command Options** - Define command-line arguments using attributes
- 🔄 **Middleware Pipeline** - Support for application middleware and lifecycle hooks
- 📦 **Multiple Application Contexts** - Support for WebApplication, ConsoleApp, and custom host builders

## Installation

```bash
dotnet add package ApplicationBuilderHelpers
```

## Quick Start

### 1. Create Your Main Program

```csharp
using ApplicationBuilderHelpers;
using YourApp.Commands;
using YourApp.Applications;

return await ApplicationBuilder.Create()
    .AddApplication<CoreApplication>()
    .AddApplication<InfrastructureApplication>()
    .AddCommand<MainCommand>()
    .AddCommand<ProcessCommand>()
    .AddCommand<MigrateCommand>()
    .RunAsync(args);
```

### 2. Define a Command

```csharp
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace YourApp.Commands;

[Command(description: "Main command for your application")]
public class MainCommand : Command
{
    [CommandOption('v', "verbose", Description = "Enable verbose output")]
    public bool Verbose { get; set; }

    [CommandOption('c', "config", Description = "Configuration file path", EnvironmentVariable = "APP_CONFIG")]
    public string? ConfigPath { get; set; }

    [CommandOption("timeout", Description = "Timeout in seconds")]
    public int Timeout { get; set; } = 30;

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        // Access services from DI container
        var logger = applicationHost.Services.GetRequiredService<ILogger<MainCommand>>();
        var myService = applicationHost.Services.GetRequiredService<IMyService>();
        
        logger.LogInformation("Running main command with verbose={Verbose}, config={Config}, timeout={Timeout}", 
            Verbose, ConfigPath, Timeout);
        
        // Your command logic here
        await myService.DoWorkAsync();
        
        cancellationTokenSource.Cancel(); // Stop the application when done
    }
}
```

### 3. Create an Application Module

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace YourApp.Applications;

public class CoreApplication : ApplicationDependency
{
    public override void CommandPreparation(ApplicationBuilder applicationBuilder)
    {
        // Add custom type parsers for command arguments
        applicationBuilder.AddCommandTypeParser<CustomTypeParser>();
    }

    public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
    {
        // Prepare the host builder before configuration
    }

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        // Add custom configuration providers or bind configuration sections
        applicationBuilder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        // Register your services
        services.AddSingleton<IMyService, MyService>();
        services.AddHttpClient();
        
        // Add other services
        services.AddLogging();
    }

    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        // Add middleware to the application pipeline
    }
    
    public override void RunPreparation(ApplicationHost applicationHost)
    {
        // Perform any final setup before the application runs (all parallel)
    }

    public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
    {
        // Perform any final setup before the application runs (all parallel)
    }
}
```

## Core Components

### ApplicationBuilder

The entry point for building your application. Provides a fluent API for composing applications and commands.

**Key Methods:**
- `Create()` - Creates a new ApplicationBuilder instance
- `AddApplication<T>()` - Adds an application module
- `AddCommand<T>()` - Registers a command
- `RunAsync(args)` - Builds and runs the application

### Command

Base class for all CLI commands. Override the `Run` method to implement command logic. Commands can also configure services and application behavior just like ApplicationDependency.

**Command Attributes:**
- `[Command]` - Defines command metadata (name, description, aliases)
- `[CommandOption]` - Defines command-line options
- `[CommandArgument]` - Defines positional arguments

**Command Lifecycle Methods:**

Commands inherit the same lifecycle methods as ApplicationDependency:

```csharp
[Command("process", description: "Process data with custom services")]
public class ProcessCommand : Command
{
    [CommandOption('f', "file", Description = "Input file path")]
    public string? FilePath { get; set; }

    // Configure command-specific services
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddScoped<IDataProcessor, DataProcessor>();
        services.AddScoped<IFileValidator, FileValidator>();
        services.AddLogging(logging => logging.AddConsole());
    }

    // Configure command-specific settings
    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        applicationBuilder.Services.Configure<ProcessingOptions>(
            configuration.GetSection("Processing"));
    }

    // Add command-specific middleware
    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        // Add performance monitoring middleware for this command
        var logger = host.Services.GetRequiredService<ILogger<ProcessCommand>>();
        logger.LogInformation("Process command middleware initialized");
    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        var processor = applicationHost.Services.GetRequiredService<IDataProcessor>();
        var validator = applicationHost.Services.GetRequiredService<IFileValidator>();
        var logger = applicationHost.Services.GetRequiredService<ILogger<ProcessCommand>>();
        
        if (await validator.ValidateAsync(FilePath))
        {
            await processor.ProcessFileAsync(FilePath);
            logger.LogInformation("Processing completed");
        }
        
        cancellationTokenSource.Cancel();
    }
}
```

**Accessing Services in Commands:**

1. **Service Locator Pattern** (through ApplicationHost):
```csharp
protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
{
    var logger = applicationHost.Services.GetRequiredService<ILogger<MyCommand>>();
    var dbContext = applicationHost.Services.GetRequiredService<AppDbContext>();
    
    logger.LogInformation("Command started");
    // Use services...
}
```

2. **Constructor Injection** (if supported by the command resolver):
```csharp
public class ImportCommand : Command
{
    private readonly IDataImporter _importer;
    private readonly ILogger<ImportCommand> _logger;
    
    public ImportCommand(IDataImporter importer, ILogger<ImportCommand> logger)
    {
        _importer = importer;
        _logger = logger;
    }
    
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        _logger.LogInformation("Importing data...");
        await _importer.ImportAsync();
        cancellationTokenSource.Cancel();
    }
}
```

### ApplicationDependency

Base class for application modules that configure services, middleware, and application behavior.

**Lifecycle Methods (shared with Command):**
1. `CommandPreparation` - Configure command parsing
2. `BuilderPreparation` - Prepare the host builder
3. `AddConfigurations` - Add configuration providers
4. `AddServices` - Register services with DI container
5. `AddMiddlewares` - Configure middleware pipeline
6. `RunPreparation` - Final setup before run (all parallel)
6. `RunPreparationAsync` - Final setup before run (all parallel)

## Advanced Features

### Custom Type Parsers

Implement custom parsing for command arguments by implementing the `ICommandTypeParser` interface:

```csharp
using ApplicationBuilderHelpers.Interfaces;
using System.Diagnostics.CodeAnalysis;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class DateTimeTypeParser : ICommandTypeParser
{
    public Type Type => typeof(DateTime);
    
    public object? Parse(string? value, out string? validateError)
    {
        validateError = null;
        
        if (string.IsNullOrEmpty(value))
        {
            validateError = "Date value cannot be empty";
            return null;
        }
        
        if (DateTime.TryParse(value, out var result))
        {
            return result;
        }
        
        validateError = $"'{value}' is not a valid date format";
        return null;
    }
    
    public string? GetString(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return value?.ToString();
    }
}

// Register in your application
public class CoreApplication : ApplicationDependency
{
    public override void CommandPreparation(ApplicationBuilder applicationBuilder)
    {
        applicationBuilder.AddCommandTypeParser<DateTimeTypeParser>();
    }
}
```

More complex type parser example:

```csharp
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class LogLevelTypeParser : ICommandTypeParser
{
    public Type Type => typeof(LogLevel);
    
    public object? Parse(string? value, out string? validateError)
    {
        validateError = null;
        
        if (string.IsNullOrWhiteSpace(value))
        {
            validateError = "Log level cannot be empty";
            return null;
        }
        
        if (Enum.TryParse<LogLevel>(value, true, out var logLevel))
        {
            return logLevel;
        }
        
        var validValues = string.Join(", ", Enum.GetNames(typeof(LogLevel)));
        validateError = $"'{value}' is not a valid log level. Valid values are: {validValues}";
        return null;
    }
    
    public string? GetString(object? value)
    {
        return value?.ToString()?.ToLower();
    }
}

// Usage in a command
[Command(description: "Configure logging")]
public class LogCommand : Command
{
    [CommandOption('l', "level", Description = "Set the logging level")]
    public LogLevel Level { get; set; } = LogLevel.Information;
    
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var logger = applicationHost.Services.GetRequiredService<ILogger<LogCommand>>();
        logger.LogInformation("Logging level set to: {Level}", Level);
        cts.Cancel();
        return ValueTask.CompletedTask;
    }
}
```

### Self-Contained Commands

Commands can be completely self-contained with their own service configuration:

```csharp
[Command("backup", description: "Backup database")]
public class BackupCommand : Command
{
    [CommandOption('o', "output", Description = "Output file path")]
    public string OutputPath { get; set; } = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";

    // Register services specific to backup operations
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<ICompressionService, CompressionService>();
        services.AddSingleton<ICloudStorageService, AzureStorageService>();
        
        // Add Azure Storage client
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddBlobServiceClient(applicationBuilder.Configuration["Azure:StorageConnection"]);
        });
    }

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        // Bind backup-specific configuration
        applicationBuilder.Services.Configure<BackupOptions>(
            configuration.GetSection("Backup"));
    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        var backupService = applicationHost.Services.GetRequiredService<IBackupService>();
        var cloudStorage = applicationHost.Services.GetRequiredService<ICloudStorageService>();
        var logger = applicationHost.Services.GetRequiredService<ILogger<BackupCommand>>();
        
        logger.LogInformation("Creating backup to {OutputPath}", OutputPath);
        var backupFile = await backupService.CreateBackupAsync(OutputPath);
        
        logger.LogInformation("Uploading backup to cloud storage");
        await cloudStorage.UploadAsync(backupFile);
        
        logger.LogInformation("Backup completed successfully");
        cancellationTokenSource.Cancel();
    }
}
```

### Sub-Commands

Create hierarchical command structures using space-separated command names:

```csharp
// Main deploy command
[Command("deploy", description: "Deployment commands")]
public class DeployCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        Console.WriteLine("Please specify a deployment target. Use --help for available options.");
        cts.Cancel();
        return ValueTask.CompletedTask;
    }
}

// Sub-command: deploy prod
[Command("deploy prod", description: "Deploy to production environment")]
public class DeployProductionCommand : Command
{
    [CommandOption("skip-tests", Description = "Skip running tests before deployment")]
    public bool SkipTests { get; set; }
    
    // Add production-specific services
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddSingleton<IDeploymentService, ProductionDeploymentService>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<IRollbackService, RollbackService>();
    }
    
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var deployService = applicationHost.Services.GetRequiredService<IDeploymentService>();
        var healthCheck = applicationHost.Services.GetRequiredService<IHealthCheckService>();
        var logger = applicationHost.Services.GetRequiredService<ILogger<DeployProductionCommand>>();
        
        logger.LogInformation("Deploying to production environment...");
        
        if (!SkipTests)
        {
            logger.LogInformation("Running pre-deployment tests...");
            // Run tests
        }
        
        await deployService.DeployAsync();
        await healthCheck.VerifyDeploymentAsync();
        
        cts.Cancel();
    }
}

// Sub-command: deploy staging
[Command("deploy staging", description: "Deploy to staging environment")]
public class DeployStagingCommand : Command
{
    // Add staging-specific services
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddSingleton<IDeploymentService, StagingDeploymentService>();
    }
    
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var deployService = applicationHost.Services.GetRequiredService<IDeploymentService>();
        var logger = applicationHost.Services.GetRequiredService<ILogger<DeployStagingCommand>>();
        
        logger.LogInformation("Deploying to staging environment...");
        await deployService.DeployAsync();
        cts.Cancel();
    }
}

// Deeper nesting: deploy prod rollback
[Command("deploy prod rollback", description: "Rollback production deployment")]
public class DeployProductionRollbackCommand : Command
{
    [CommandOption("version", Description = "Specific version to rollback to")]
    public string? Version { get; set; }
    
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var deployService = applicationHost.Services.GetRequiredService<IDeploymentService>();
        var logger = applicationHost.Services.GetRequiredService<ILogger<DeployProductionRollbackCommand>>();
        
        logger.LogInformation("Rolling back production to version {Version}...", Version ?? "previous");
        await deployService.RollbackProductionAsync(Version);
        cts.Cancel();
    }
}
```

Usage examples:
```bash
# Main deploy command
myapp deploy

# Deploy to production
myapp deploy prod

# Deploy to staging
myapp deploy staging

# Rollback production
myapp deploy prod rollback --version 1.2.3
```

### Multiple Host Types

ApplicationBuilderHelpers supports different host types:

- `HostApplicationBuilder` - For console applications
- `WebApplicationBuilder` - For web applications
- Custom builders implementing `IHostApplicationBuilder`

### Environment-Specific Configuration

```csharp
public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
{
    var environment = applicationBuilder.Environment;
    
    if (environment.IsDevelopment())
    {
        // Development-specific configuration
    }
    else if (environment.IsProduction())
    {
        // Production-specific configuration
    }
}
```

## Architecture

ApplicationBuilderHelpers follows a modular architecture pattern:

```
┌─────────────────────┐
│  ApplicationBuilder │ ← Entry Point
└──────────┬──────────┘
           │
    ┌──────▼──────┐
    │  Commands   │ ← Command Registration (can also configure services)
    └──────┬──────┘
           │
    ┌──────▼──────────┐
    │  Applications   │ ← Application Modules
    └──────┬──────────┘
           │
    ┌──────▼───────────┐
    │  Host Builder    │ ← Host Configuration
    └──────┬───────────┘
           │
    ┌──────▼──────┐
    │  Services   │ ← Dependency Injection (from both Commands and Applications)
    └──────┬──────┘
           │
    ┌──────▼──────────┐
    │  Middleware     │ ← Request Pipeline
    └──────┬──────────┘
           │
    ┌──────▼──────┐
    │  Execution  │ ← Command Execution
    └─────────────┘
```

## Best Practices

1. **Separation of Concerns** - Keep commands focused on coordination, move business logic to services
2. **Use Application Modules** - Group related configurations and services in ApplicationDependency classes
3. **Command-Specific Services** - Register services in commands when they're only needed for that specific command
4. **Leverage DI** - Use dependency injection for better testability and maintainability
5. **Configuration Over Code** - Use configuration files for environment-specific settings
6. **Async All The Way** - Use async/await patterns for I/O operations
7. **Prefer Service Locator in Commands** - When constructor injection isn't available, use `applicationHost.Services` to access dependencies
8. **Logical Command Hierarchy** - Use space-separated command names for intuitive sub-command structures
9. **Validate in Type Parsers** - Return meaningful error messages from custom type parsers to help users

## Examples

### Simple Console Application

```csharp
// Program.cs
return await ApplicationBuilder.Create()
    .AddCommand<GreetCommand>()
    .RunAsync(args);

// GreetCommand.cs
[Command(description: "Greet someone")]
public class GreetCommand : Command
{
    [CommandArgument(0, Description = "Name to greet")]
    public string Name { get; set; } = "World";
    
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        Console.WriteLine($"Hello, {Name}!");
        cts.Cancel();
        return ValueTask.CompletedTask;
    }
}
```

### Command with Its Own Services

```csharp
// Program.cs
return await ApplicationBuilder.Create()
    .AddCommand<ReportCommand>()
    .RunAsync(args);

// ReportCommand.cs
[Command("report", description: "Generate reports")]
public class ReportCommand : Command
{
    [CommandOption('f', "format", Description = "Report format (pdf, excel, html)")]
    public string Format { get; set; } = "pdf";
    
    [CommandOption('o', "output", Description = "Output directory")]
    public string OutputDir { get; set; } = "./reports";

    // Register report-specific services
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddScoped<IReportGenerator, ReportGenerator>();
        services.AddScoped<IPdfRenderer, PdfRenderer>();
        services.AddScoped<IExcelRenderer, ExcelRenderer>();
        services.AddScoped<IHtmlRenderer, HtmlRenderer>();
        services.AddScoped<IDataRepository, DataRepository>();
        
        // Add logging specifically for report generation
        services.AddLogging(logging => 
        {
            logging.AddConsole();
            logging.AddFile("logs/reports-{Date}.txt");
        });
    }

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        // Bind report configuration
        applicationBuilder.Services.Configure<ReportOptions>(
            configuration.GetSection("Reporting"));
    }

    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        var generator = applicationHost.Services.GetRequiredService<IReportGenerator>();
        var logger = applicationHost.Services.GetRequiredService<ILogger<ReportCommand>>();
        
        logger.LogInformation("Generating {Format} report to {OutputDir}", Format, OutputDir);
        
        var report = await generator.GenerateAsync(Format);
        await report.SaveToAsync(OutputDir);
        
        logger.LogInformation("Report generated successfully");
        cts.Cancel();
    }
}
```

### Complex Application with Command Hierarchy

```csharp
// Program.cs
return await ApplicationBuilder.Create()
    .AddApplication<CoreApplication>()
    .AddApplication<InfrastructureApplication>()
    .AddCommand<MainCommand>()
    .AddCommand<DbCommand>()
    .AddCommand<DbMigrateCommand>()
    .AddCommand<DbSeedCommand>()
    .AddCommand<DbBackupCommand>()
    .RunAsync(args);

// Database commands hierarchy
[Command("db", description: "Database management commands")]
public class DbCommand : Command
{
    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cts)
    {
        Console.WriteLine("Database management commands. Use --help for available sub-commands.");
        cts.Cancel();
        return ValueTask.CompletedTask;
    }
}

[Command("db migrate", description: "Run database migrations")]
public class DbMigrateCommand : Command
{
    [CommandOption("connection-string", Description = "Database connection string", EnvironmentVariable = "DB_CONNECTION")]
    public string? ConnectionString { get; set; }
    
    [CommandOption("dry-run", Description = "Preview migrations without applying")]
    public bool DryRun { get; set; }
    
    // Add migration-specific services
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = ConnectionString ?? applicationBuilder.Configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
        });
        
        services.AddScoped<IDatabaseMigrator, EfCoreMigrator>();
    }
    
    protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        var logger = applicationHost.Services.GetRequiredService<ILogger<DbMigrateCommand>>();
        var migrator = applicationHost.Services.GetRequiredService<IDatabaseMigrator>();
        
        logger.LogInformation("Starting database migration. Dry run: {DryRun}", DryRun);
        
        if (DryRun)
        {
            var pending = await migrator.GetPendingMigrationsAsync();
            logger.LogInformation("Pending migrations: {Migrations}", string.Join(", ", pending));
        }
        else
        {
            await migrator.MigrateAsync();
            logger.LogInformation("Database migration completed successfully");
        }
        
        cancellationTokenSource.Cancel();
    }
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or suggestions, please create an issue on the [GitHub repository](https://github.com/Kiryuumaru/ApplicationBuilderHelpers).