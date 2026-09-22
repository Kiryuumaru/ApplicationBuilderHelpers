# Custom Type Parsers

Type parsers convert between command-line strings and typed property values. The library ships with 24 built-in parsers, but you can add custom ones for any type.

## Interface

`ICommandTypeParser` requires:

```csharp
public interface ICommandTypeParser
{
    Type Type { get; }
    object? Parse(string? value, out string? validateError);
    string? GetString(object? value);
    object? GetDefaultValue();
    Array CreateTypedArray(int length);
    IList CreateTypedList(int capacity);
}
```

## Culture Policy (InvariantCulture)

CLI text is always parsed with `CultureInfo.InvariantCulture`, regardless of `CurrentCulture`. The decimal separator is always `.`, with explicit styles per type (integers: `NumberStyles.Integer`; `decimal`: `NumberStyles.Number`; `float`/`double`: `NumberStyles.Float | NumberStyles.AllowThousands`; date/time: `DateTimeStyles.AllowWhiteSpaces`). The `ICommandTypeParser.Parse` signatures are unchanged.

Custom `ICommandTypeParser` authors MUST use the `InvariantCulture` provider overloads:

```csharp
using System.Globalization;

if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
    return result;
```

Display/output via `GetString` / `GetStringValue` (which default to `ToString()`) may still follow `CurrentCulture` — that direction is intentionally out of scope.

## Using `CommandTypeParser<T>`

Inherit from the abstract base class for a simpler implementation:

```csharp
using ApplicationBuilderHelpers.Abstracts;
using System.Globalization;

public class DateTimeTypeParser : CommandTypeParser<DateTime>
{
    public override DateTime? ParseValue(string? value, out string? validateError)
    {
        validateError = null;
        if (string.IsNullOrEmpty(value))
        {
            validateError = "Date value cannot be empty";
            return null;
        }
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
            return result;
        validateError = $"'{value}' is not a valid date format";
        return null;
    }

    public override string? GetStringValue(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public override DateTime? GetDefaultValue() => null;

    public override Array CreateTypedArray(int length) => new DateTime[length];

    public override IList CreateTypedList(int capacity) => new List<DateTime>(capacity);
}
```

## Implementing `ICommandTypeParser` Directly

For maximum control, implement the interface directly:

```csharp
using System.Globalization;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class TimeSpanTypeParser : ICommandTypeParser
{
    public Type Type => typeof(TimeSpan);

    public object? Parse(string? value, out string? validateError)
    {
        validateError = null;
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result))
            return result;
        validateError = $"'{value}' is not a valid time span";
        return null;
    }

    public string? GetString(object? value)
        => value?.ToString();

    public object? GetDefaultValue()
        => TimeSpan.Zero;

    public Array CreateTypedArray(int length)
        => new TimeSpan[length];

    public IList CreateTypedList(int capacity)
        => new List<TimeSpan>(capacity);
}
```

## Registration

Register in `CommandPreparation` or directly on the builder:

```csharp
// In ApplicationDependency
public override void CommandPreparation(ApplicationBuilder applicationBuilder)
{
    applicationBuilder.AddCommandTypeParser<DateTimeTypeParser>();
}

// Or in Program.cs
ApplicationBuilder.Create()
    .AddCommandTypeParser<DateTimeTypeParser>()
    .AddCommand<MyCommand>()
    .RunAsync(args);
```

Parsers added between runs are visible on the next `RunAsync` (topology and enum `FromAmong` resolution read the live collection); registering a custom parser for an enum type suppresses the automatic enum-value population for both options and positional arguments.

## Built-in Parsers

These are registered automatically and can be overridden:

`AbsolutePath`, `bool`, `byte`, `char`, `DateOnly`, `DateTime`, `DateTimeOffset`, `decimal`, `double`, `FileInfo`, `float`, `Guid`, `int`, `long`, `sbyte`, `short`, `string`, `TimeOnly`, `TimeSpan`, `uint`, `ulong`, `Uri`, `ushort`, `Version`

## Collection Binding

Repeatable options and multi-value arguments bind per element through the same pipeline. Supported shapes are `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, and `IList<T>` (interface shapes materialize as `List<T>`):

```csharp
[CommandOption("tag", Description = "Repeatable tags.")]
public List<string>? Tags { get; set; }

[CommandArgument("file", Description = "Input files.", Position = 0)]
public IEnumerable<FileInfo>? Files { get; set; }
```

```sh
myapp build --tag=a --tag=b file1.txt file2.txt
```

## FromAmong Validation

`FromAmong` allowed values are compared after conversion (convert-then-compare): the CLI text is parsed to the property type first, then the converted value is compared against the allowed entries (string entries for the same type are parsed before comparison, and non-string entries are normalized into the target type before comparison). Equivalent representations therefore match — `--level=02` satisfies `FromAmong = [1, 2, 3]`, and `--mode=0` matches an enum entry with value `0`. Non-string numeric entries normalize through the target type (`ChangeType` for numerics, `Enum.ToObject` for enums), but for non-`Flags` enums only defined enum values are matchable: with `FromAmong = [0, 1, 2, 3]` on an enum with `Low = 0, High = 1`, inputs `0` and `1` accept while `2` and `3` report `Must be one of: ...`. Unconvertible, overflowing, or inexactly-representable (e.g. `2.5` vs `int`) candidates never match. Enum targets with a custom parser keep exact `Equals` comparison. When conversion itself fails and the raw text matches no allowed display string, the error reports the allowed list (`Must be one of: ...`) instead of a bare invalid-value error, so unparseable enum input such as `--color=Purple` still lists the allowed values. The same rule applies to positional arguments: a plain-enum argument auto-populates its allowed list from the enum names, shows the full list in help and completion, and is suppressed symmetrically when a custom parser is registered for that enum type.
