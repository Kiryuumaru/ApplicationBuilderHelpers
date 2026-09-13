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
}
```

## Using `CommandTypeParser<T>`

Inherit from the abstract base class for a simpler implementation:

```csharp
using ApplicationBuilderHelpers.Abstracts;

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
        if (DateTime.TryParse(value, out var result))
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
}
```

## Implementing `ICommandTypeParser` Directly

For maximum control, implement the interface directly:

```csharp
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class TimeSpanTypeParser : ICommandTypeParser
{
    public Type Type => typeof(TimeSpan);

    public object? Parse(string? value, out string? validateError)
    {
        validateError = null;
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (TimeSpan.TryParse(value, out var result))
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

Parsers added between runs are visible on the next `RunAsync` (topology and enum `FromAmong` resolution read the live collection); registering a custom parser for an enum type suppresses the automatic enum-value population.

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

`FromAmong` allowed values are compared after conversion (convert-then-compare): the CLI text is parsed to the property type first, then the converted value is compared against the allowed entries (string entries for the same type are parsed before comparison). Equivalent representations therefore match — `--level=02` satisfies `FromAmong = [1, 2, 3]`, and `--mode=0` matches an enum entry with value `0`. When conversion itself fails and the raw text matches no allowed display string, the error reports the allowed list (`Must be one of: ...`) instead of a bare invalid-value error, so unparseable enum input such as `--color=Purple` still lists the allowed values.
