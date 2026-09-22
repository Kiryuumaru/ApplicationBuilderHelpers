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
    .SetHelpWidth(120)       // Line width for help output (must be positive; 0 and negatives throw)
    .SetHelpBorderWidth(2)   // Border indentation
    // ...
```

`SetHelpWidth` requires a positive width — `0` and negatives throw `ArgumentOutOfRangeException`. When unset, help output defaults to `120` columns. The formatter floors the effective width at `60` columns (`20` minimum left column + `40` reserved for the right column) to keep two-column help readable at narrow widths. `80` is a common console-width convention you may pass explicitly; the code default when unset remains `120`.

### Help Placeholder Tokens (#454)

Option value placeholders come from a single mapper
(`src/ApplicationBuilderHelpers/CommandLineParser/HelpTypeDisplay.cs:8-47`).
Help never prints raw CLR names (`TimeSpan`, `Guid`, `Uri`, `Nullable<T>`).

| Token | Types |
|---|---|
| `STRING` | `string` |
| `NUMBER` | `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal` (scalar `decimal` renders `--opt <NUMBER>`) |
| `DATE` | `DateTime`, `DateOnly`, `TimeOnly`, `DateTimeOffset` |
| `FILE` | `FileInfo`, `AbsolutePath` |
| `DIR` | `DirectoryInfo` |
| `VALUE` | everything else, including enums, `TimeSpan`, `Guid`, `Uri`, `Version`, `char` |

Rules:

- `Nullable<T>` unwraps to `T` before mapping (`HelpTypeDisplay.cs:12` for scalars, `:59` for collection elements); e.g. `int?` → `<NUMBER>`.
- `bool` / `bool?` are flags (`src/ApplicationBuilderHelpers/CommandLineParser/SubCommandOptionInfo.cs:87`) and show no placeholder (`HelpTypeDisplay.cs:51-52,67-68`): `--verbose`, never `--verbose <BOOL>`.
- Collections render `<TOKEN...>` from the element type (`HelpTypeDisplay.cs:55-65`): `List<string>` → `<STRING...>`, `List<decimal>` → `<NUMBER...>`.
- Enums render generic `<VALUE>` plus a `Possible values: ...` line (`src/ApplicationBuilderHelpers/CommandLineParser/HelpFormatter.cs:416-420`).
- Positional arguments show name-only (`src/ApplicationBuilderHelpers/CommandLineParser/HelpFormatter.cs:438-448`): `<name>` / `[name]`, no type token.

### Required Option Help Descriptions (#507)

Required options render the verbatim lowercase `(required)` marker on the line immediately after the description, from `BuildOptionDescription` (`src/ApplicationBuilderHelpers/CommandLineParser/HelpContentProvider.cs:288-322`). Fixed ordinal: Description (`:292-295`) → `(required)` (`:297-300`) → `Possible values: ...` (`:302-306`) → `Environment variable: ...` (`:308-311`). No `Default:` line when `IsRequired` (`:313-318`).

- A required `int` omits the phantom `Default: 0` (`src/ApplicationBuilderHelpers.Test.Cli.UnitTest/RequiredOptionHelpTests.cs:47-58`); a required `string` shows the marker with no `Default:` line (`:61-72`); an optional `int` with an explicit initializer keeps its `Default:` line (e.g. `Default: 3`, `:75-86`).
- A required option with `FromAmong` plus `EnvironmentVariable` renders Description → `(required)` → `Possible values:` → `Environment variable:` (`:89-102`).
- Secret interplay: suppression beats redaction — a required secret option never shows `Default: [REDACTED]`, because the `Default:` arm is skipped before `SecretRedaction.GetDefaultDisplay` (`HelpContentProvider.cs:313-317`; mask at `src/ApplicationBuilderHelpers/CommandLineParser/SecretRedaction.cs:40-46`).
- Signatures, usage `[OPTIONS]`, and layout are unchanged — only description lines change. Option-side contract (Required property, env/secret interplay): [Commands](commands.md#required-options-in-help).

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

Resolution reads the key's direct value first: a plain value wins, while an `@ref:` value triggers one hop, repeating until a terminal (non-`@ref:`) value or the bound. Chains resolve at most 32 hops (case-insensitive); a cycle (revisiting a key) or overflow (exceeding the depth) fails to resolve.

Throws `NoConfigValueException` if a reference can't be resolved.
