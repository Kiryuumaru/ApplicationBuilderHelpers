# Custom Type Parsers

Type parsers convert command-line text into typed option and argument values. The library ships with 24 built-in parsers. Add a custom one when you need your own type.

## When You Need One

If your option uses `int`, `string`, `DateTime`, `FileInfo`, `Uri`, `Guid`, `TimeSpan`, `Version`, `AbsolutePath`, or the other built-in types, you already have a parser. You need a custom parser only for your own domain types, for example `Currency` or `Duration`.

```csharp
[Command("invoice", description: "Bill a customer")]
public class InvoiceCommand : Command
{
    [CommandOption("total", Description = "Amount to bill.")]
    public Currency? Total { get; set; }

    protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Billing {Total}");
        return ValueTask.CompletedTask;
    }
}
```

Without a `Currency` parser this fails. The two sections below show how to write one.

## The Easy Way: Extend `CommandTypeParser<T>`

Override `ParseValue`. Override `GetStringValue` only to change how the value prints in help defaults. You rarely need to touch collection storage: the defaults already build `T[]` and `List<T>`.

```csharp
using ApplicationBuilderHelpers.Abstracts;
using System.Globalization;

public class CurrencyTypeParser : CommandTypeParser<Currency>
{
    public override Currency? ParseValue(string? value, out string? validateError)
    {
        validateError = null;
        if (string.IsNullOrEmpty(value))
        {
            validateError = "Currency value cannot be empty";
            return null;
        }
        if (Currency.TryParse(value, CultureInfo.InvariantCulture, out var result))
            return result;
        validateError = $"'{value}' is not a valid currency";
        return null;
    }

    public override string? GetStringValue(Currency? value)
    {
        return value?.ToString("C", CultureInfo.InvariantCulture);
    }
}
```

On failure, return `null` and set `validateError` to the reason. That reason appears in the `InvalidValue` error (exit 2).

The `?` spellings above suit class targets. If your type is a struct, drop the `?`.

```csharp
using ApplicationBuilderHelpers.Abstracts;
using System.Globalization;

public class DurationTypeParser : CommandTypeParser<Duration>
{
    public override Duration ParseValue(string? value, out string? validateError)
    {
        validateError = null;
        if (string.IsNullOrEmpty(value))
        {
            validateError = "Duration value cannot be empty";
            return default;
        }
        if (Duration.TryParse(value, CultureInfo.InvariantCulture, out var result))
            return result;
        validateError = $"'{value}' is not a valid duration";
        return default;
    }

    public override string? GetStringValue(Duration value)
    {
        return value.ToString();
    }
}
```

On failure, return `default` and set `validateError`. The `?` overrides do not compile for struct targets.

## The Full-Control Way: Implement `ICommandTypeParser`

Implement the interface directly when you need control over defaults and collection storage:

```csharp
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

public class DurationTypeParser : ICommandTypeParser
{
    public Type Type => typeof(Duration);

    public object? Parse(string? value, out string? validateError)
    {
        validateError = null;
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Duration.TryParse(value, CultureInfo.InvariantCulture, out var result))
            return result;
        validateError = $"'{value}' is not a valid duration";
        return null;
    }

    public string? GetString(object? value)
        => value?.ToString();

    public object? GetDefaultValue()
        => Duration.Zero;

    public Array CreateTypedArray(int length)
        => new Duration[length];

    public IList CreateTypedList(int capacity)
        => new List<Duration>(capacity);
}
```

The interface has 6 members: `Type`, `Parse`, `GetString`, `GetDefaultValue`, `CreateTypedArray`, and `CreateTypedList`.

## Registering Your Parser

Register once and every command can use the type:

```csharp
// In Program.cs — simplest for one app
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;

ApplicationBuilder.Create()
    .AddCommandTypeParser<CurrencyTypeParser>()
    .AddCommand<InvoiceCommand>()
    .RunAsync(args);
```

```csharp
// In a shared setup module — reuse across commands
using ApplicationBuilderHelpers;

public override void CommandPreparation(ApplicationBuilder applicationBuilder)
{
    applicationBuilder.AddCommandTypeParser<CurrencyTypeParser>();
}
```

Parsers added between runs apply on the next `RunAsync`.

## Parsing Rules

Always parse numbers and dates with `CultureInfo.InvariantCulture`, regardless of the machine's locale. CLI text always arrives in invariant culture:

```csharp
if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
    return result;
```

Keep `Parse` pure: no side effects, and the same input always gives the same output and error. Binding may run your parser twice per value (once to check, once to bind).

Registering a custom parser for an enum type turns off automatic enum-value help for that enum, for options and arguments alike.

## Built-in Types

These 24 types already have parsers. You can override any of them by registering your own parser for the same type:

`AbsolutePath`, `bool`, `byte`, `char`, `DateOnly`, `DateTime`, `DateTimeOffset`, `decimal`, `double`, `FileInfo`, `float`, `Guid`, `int`, `long`, `sbyte`, `short`, `string`, `TimeOnly`, `TimeSpan`, `uint`, `ulong`, `Uri`, `ushort`, `Version`

## Repeatable Options and Multi-Value Arguments

Repeat an option to fill a collection. Supported shapes are `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, and `IList<T>`:

```csharp
[CommandOption("tag", Description = "Repeatable tags.")]
public List<string>? Tags { get; set; }

[CommandArgument("file", Description = "Input files.", Position = 0)]
public IEnumerable<FileInfo>? Files { get; set; }
```

```sh
myapp build --tag=a --tag=b file1.txt file2.txt
# exit 0
```

## Limiting Values With `FromAmong`

Restrict an option to an allowed list. The library converts the typed text first, then compares, so equivalent spellings match: `--level=02` satisfies `FromAmong = [1, 2, 3]`.

```csharp
[CommandOption("level", Description = "Build level.", FromAmong = new[] { 1, 2, 3 })]
public int Level { get; set; }
```

When the value is not allowed, the error names the list: `Must be one of: ...`. The same rule applies to positional arguments. Plain enums fill their allowed list from the enum names automatically. A matched string value binds in the allowed list's casing: `--mode=JSON` binds as `json` when `FromAmong` lists `json`.
