# Configuration & Themes

## Fluent Configuration

`ApplicationBuilder` supports fluent configuration methods:

### Executable Metadata

Override auto-detected assembly metadata:

```csharp
ApplicationBuilder.Create()
    .SetExecutableName("myapp")
    .SetExecutableTitle("My Application")
    .SetExecutableDescription("Does awesome things")
    .SetExecutableVersion("2.0.0")
    .AddCommand<MyCommand>()
    .RunAsync(args);
```

All four are optional. When not set, the library auto-detects from the entry assembly's attributes.

### Help Formatting

```csharp
ApplicationBuilder.Create()
    .SetHelpWidth(120)       // Line width for help output
    .SetHelpBorderWidth(2)   // Border indentation
    // ...
```

## Console Themes

The library includes 5 built-in themes implementing `IConsoleTheme`:

| Theme | Header | Flag | Parameter | Description |
|---|---|---|---|---|
| `DefaultConsoleTheme` | Yellow | Green | Cyan | White |
| `MonochromeConsoleTheme` | White | Gray | DarkGray | White |
| `MinimalConsoleTheme` | Blue | DarkCyan | DarkBlue | Gray |
| `DarkConsoleTheme` | Magenta | Green | Cyan | White |
| `HighContrastConsoleTheme` | White | Yellow | Cyan | White |

Each theme exposes 6 color properties: `HeaderColor`, `FlagColor`, `ParameterColor`, `DescriptionColor`, `SecondaryColor`, `RequiredColor`.

### Setting a Theme

```csharp
// Built-in via static Instance
ApplicationBuilder.Create()
    .SetTheme(DarkConsoleTheme.Instance)
    // ...

// Or create a custom theme
ApplicationBuilder.Create()
    .SetTheme(new MyCustomTheme())
    // ...
```

## Configuration Reference System (`@ref:`)

Configuration values can reference other keys:

```json
{
  "Environment": "production",
  "ProductionDb": "Server=prod;Database=main",
  "StagingDb": "Server=staging;Database=main",
  "ConnectionString": "@ref:ProductionDb"
}
```

### API

```csharp
// Resolve references (follows chains)
string connStr = configuration.GetRefValue("ConnectionString");

// Check if value exists
bool exists = configuration.ContainsRefValue("ConnectionString");

// Get with fallback
string? value = configuration.GetRefValueOrDefault("Missing", "default-value");

// Try pattern
if (configuration.TryGetRefValue("ConnectionString", out var resolved))
{
    // resolved contains the final value
}
```

References can be chained: `"A": "@ref:B"` → `"B": "@ref:C"` → `"C": "actual-value"`.

Throws `NoConfigValueException` if a reference can't be resolved.
