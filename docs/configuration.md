# Configuration & Themes

Set your app name, shape your help output, pick a color theme, and reuse one setting inside another.

Start with executable metadata, then help width, then a theme. Reach for `@ref:` only when one setting must reuse another.

## Executable Metadata

All four setters are optional. When you skip them, the library detects values from your entry assembly's attributes.

```csharp
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
ApplicationBuilder.Create()
    .SetHelpWidth(120);
```

`SetHelpWidth` needs a positive number. `0` and negatives throw `ArgumentOutOfRangeException`.

When you skip it, help renders at 120 columns. Narrow output never squeezes below 60 columns, so two-column help stays readable.

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

A required option never shows a `Default:` line. An optional option with a starting value keeps its `Default:` line, for example `Default: 3`. See [Commands](commands.md#options).

## Console Themes

The library ships 6 built-in color themes. Start with `DefaultConsoleTheme` unless the terminal needs something else.

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
string connStr = configuration.GetRefValue("ConnectionString");

if (configuration.TryGetRefValue("ConnectionString", out var resolved))
{
    // resolved holds the final value
}
```

References can chain: `"A"` points at `"B"`, `"B"` points at `"C"`, `"C"` holds the real value. Matching ignores case. Chains resolve at most 32 hops. A cycle or a missing key fails to resolve instead of looping.
