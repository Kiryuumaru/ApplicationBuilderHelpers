# Configuration & Themes

Set your app name, shape your help output, pick a color theme, and reuse one setting inside another.

Start with executable metadata, then help width, then a theme. Reach for `@ref:` only when one setting must reuse another.

## Executable Metadata

All four setters are optional. When you skip them, the library detects values from your entry assembly's attributes.

```csharp
// Program.cs
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;

ApplicationBuilder.Create()
    .SetExecutableName("myapp")
    .SetExecutableTitle("My Application")
    .SetExecutableDescription("Does awesome things")
    .SetExecutableVersion("2.0.0")
    .AddCommand<MyCommand>()
    .RunAsync(args);
```

| Setter | Controls |
|---|---|
| `SetExecutableName` | Name shown in help and error footers |
| `SetExecutableTitle` | Title shown in help headers |
| `SetExecutableDescription` | Description shown in help |
| `SetExecutableVersion` | Version printed by `--version` |

## Help Width

```csharp
// Program.cs
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;

ApplicationBuilder.Create()
    .SetHelpWidth(120);
```

`SetHelpWidth` needs a number from 1 to 1024. `0`, negatives, and values above 1024 throw `ArgumentOutOfRangeException`.

When you skip it, help renders at 120 columns. Values below 60 render at 60, so two-column help stays readable. Values above 1024 render at 1024.

## Help Placeholders

Help never prints raw type names. Each option shows a placeholder for the value it wants, like `--output <FILE>`.

| Placeholder | Option types |
|---|---|
| `STRING` | `string` |
| `NUMBER` | `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal` |
| `DATE` | `DateTime`, `DateOnly`, `TimeOnly`, `DateTimeOffset` |
| `FILE` | `FileInfo`, `AbsolutePath` |
| `DIR` | `DirectoryInfo` |
| `VALUE` | Everything else, including enums, `TimeSpan`, `Guid`, `Uri`, `Version`, `char` |

Rules you will see in practice:

- `int?` renders like `int`. Nullable types unwrap before mapping.
- `bool` options are flags and show no placeholder: `--verbose`, never `--verbose <BOOL>`.
- Repeatable options show the element type plus `...`: `List<string>` renders `<STRING...>`.
- Enums render `<VALUE>` plus a `Possible values:` line.
- Positional arguments show only the name: `<NAME>` or `[NAME]`.

## Required Options in Help

Required options print a lowercase `(required)` marker on the line after the description. The order is always description, then `(required)`, then `Possible values:`, then `Environment variable:`.

A required option never shows a `Default:` line. An optional option with a starting value keeps its `Default:` line, for example `Default: 3`. A secret optional always shows `Default: [REDACTED]` to signal secrecy, not that a default exists. See [Commands](commands.md#options).

## Console Themes

The library ships 6 built-in color themes. Start with `DefaultConsoleTheme` unless the terminal needs something else. Piped or redirected output renders plain; themes apply to interactive terminals only.

| Theme | Header | Flag | Value | Text |
|---|---|---|---|---|
| `DefaultConsoleTheme` | Yellow | Green | Cyan | White |
| `MonochromeConsoleTheme` | White | Gray | DarkGray | White |
| `HighContrastConsoleTheme` | Yellow | Cyan | Magenta | White |
| `MinimalConsoleTheme` | Blue | DarkCyan | DarkBlue | Gray |
| `DarkConsoleTheme` | Magenta | Green | Cyan | White |
| `LightConsoleTheme` | DarkBlue | DarkGreen | DarkCyan | Black |

### Setting a Theme

```csharp
// Program.cs
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Themes;

// Use a built-in theme
ApplicationBuilder.Create()
    .SetTheme(DarkConsoleTheme.Instance);

// Or pick a theme by type
ApplicationBuilder.Create()
    .SetTheme<DarkConsoleTheme>();
```

### Writing a Custom Theme

Implement `IConsoleTheme` with 6 colors and pass an instance to `SetTheme`:

```csharp
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Interfaces;

public class MyCustomTheme : IConsoleTheme
{
    public ConsoleColor HeaderColor => ConsoleColor.Yellow;
    public ConsoleColor FlagColor => ConsoleColor.Green;
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;
    public ConsoleColor DescriptionColor => ConsoleColor.White;
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

ApplicationBuilder.Create()
    .SetTheme(new MyCustomTheme());
```

`HeaderColor` paints section headers. `FlagColor` paints command and option names. `ParameterColor` paints value placeholders. `DescriptionColor` paints main text. `SecondaryColor` paints defaults and secondary info. `RequiredColor` paints required markers and errors.

## Reusing One Setting Inside Another (`@ref:`)

Point one setting at another key instead of copying the value:

```json
{
  "Environment": "production",
  "ProductionDb": "Server=prod;Database=main",
  "StagingDb": "Server=staging;Database=main",
  "ConnectionString": "@ref:ProductionDb"
}
```

| Method | Use it to |
|---|---|
| `GetRefValue("ConnectionString")` | Follow the chain and return the final value. Throws `NoConfigValueException` when nothing resolves. |
| `TryGetRefValue("ConnectionString", out var resolved)` | Follow the chain. Returns `true` and sets `resolved` on success. |
| `ContainsRefValue("ConnectionString")` | Check whether the key resolves. |
| `GetRefValueOrDefault("Missing", "fallback")` | Return the resolved value, or `"fallback"` when nothing resolves. |

```csharp
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;

string connStr = configuration.GetRefValue("ConnectionString");

if (configuration.TryGetRefValue("ConnectionString", out var resolved))
{
    // resolved holds the final value
}
```

References can chain: `"A"` points at `"B"`, `"B"` points at `"C"`, `"C"` holds the real value. Matching ignores case. Chains resolve at most 32 hops. A cycle or a missing key fails to resolve instead of looping.

## `@ref:` Versus `@file`

`@ref:` reuses app settings. `@file` splices command-line words. They never cross.

| Marker | Where it works | What it does |
|---|---|---|
| `@ref:OtherKey` | Settings values only | Copies another setting |
| `@path` | Command-line words only | Splices file words before parsing |

Do not put `@file` in settings. Do not put `@ref:` on the command line. Response-file rules live in [Commands](commands.md#response-files-file).
