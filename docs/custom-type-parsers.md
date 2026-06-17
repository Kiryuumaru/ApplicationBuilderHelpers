# Custom Type Parsers

Type parsers convert between command-line strings and typed property values. The library ships with 18 built-in parsers, but you can add custom ones for any type.

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

## Built-in Parsers

These are registered automatically and can be overridden:

`AbsolutePath`, `bool`, `byte`, `char`, `DateTime`, `DateTimeOffset`, `decimal`, `double`, `float`, `Guid`, `int`, `long`, `sbyte`, `short`, `string`, `uint`, `ulong`, `ushort`
