# Custom Type Parsers

Type parsers convert command-line text into typed option and argument values. The library ships with 25 built-in parsers. Add a custom one when you need your own type.

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

Override `ParseValue`. Override `GetStringValue` only to change how the value prints in help defaults. You rarely need to touch collection storage: the defaults already build `T[]` and `List<T>`. The base `GetDefaultValue()` is not virtual and returns `default(T)`, so implement `ICommandTypeParser` when an omitted option needs a non-default value.

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

Implement the interface directly when you need a non-default value for omitted input or control over collection storage:

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
        => Duration.FromSeconds(30); // Must differ from default(Duration) to bind.

    public Array CreateTypedArray(int length)
        => new Duration[length];

    public IList CreateTypedList(int capacity)
        => new List<Duration>(capacity);
}
```

The interface has 6 members: `Type`, `Parse`, `GetString`, `GetDefaultValue`, `CreateTypedArray`, and `CreateTypedList`.

`GetDefaultValue` supplies the value for an omitted optional valued option. Return a fresh instance when the type is mutable: the library stores the returned object on the property and calls the method once per option per run. Returning `null` means no parser default; a throwing `GetDefaultValue` and a value that is not runtime-assignable to the option type are treated the same way.

## Defaults for Omitted Options

An omitted optional valued option resolves its value in this order:

1. An explicit CLI value, including `""`.
2. A non-blank environment variable fallback.
3. A non-CLR-default property initializer.
4. The registered parser default for the property's exact type, when runtime-assignable and allowed by `FromAmong`.
5. The CLR default.

Required options, flags, and collections never receive parser defaults. A required option still fails as missing when no explicit or env value exists. A flag keeps its bare or initializer value. A collection is not filled from a parser default.

On a non-nullable value type, an initializer that writes the CLR default (`= 0`, `= false`, a zero-valued enum such as `= MyEnum.None`, or a zero-valued struct such as `= new MyStruct(0)`) is indistinguishable from no initializer, so the parser default applies. On a nullable value type (`int?`, `bool?`), only `= null` is indistinguishable; `= 0` and `= false` are real initializers and win. On a reference type, only `= null` is indistinguishable; any non-null initializer wins. `= ""` is a real initializer even though help prints no `Default:` line for it.

An explicit invalid value still fails validation. The parser default is not a fallback for bad input.

A parser default that is itself the CLR default or an empty value is a no-op: it neither binds nor contributes a `Default:` line. Return a non-default value when the default matters.

`FromAmong` is checked only after the type gate. A parser default that is not runtime-assignable to the option type is ignored first: no binding, no parser `Default:` line, no error. A parser default outside the allowed list is likewise ignored; the property keeps its initializer or CLR default. Typing that outside-list value explicitly still fails with `Must be one of: ...`.

Parser defaults use the property's exact type. A `Port?` property does not borrow a parser registered for `Port`; register a parser for `Port?` when you need a nullable default. A parser default of `null` is a no-op; the property keeps its current value.

A secret optional valued option still binds its parser default; help keeps showing `Default: [REDACTED]`.

An inherited option uses the same rules. See [Inherited Options](commands.md#inherited-options) for override, shadow, and private-base behavior.

## Formatting Help Defaults

When the option type has a registered parser, `GetString` formats the `Default:` line for both initializer values and parser defaults. Return `null`, an empty string, or whitespace to suppress the line. If `GetString` throws, the line is suppressed and help still renders. The library replaces control characters and the U+2028/U+2029 line separators with spaces before display.

Help and binding share one resolver per run, so they use the same parser default and registration snapshot. The library calls `GetDefaultValue` once per option per run. On a shared instance registration, help can use the live value after an explicit CLI or environment binding; see [Commands](commands.md#shared-instances-and-help).

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

Registering a custom parser for an enum type turns off automatic enum-value help for that enum on scalar options and arguments.

## Built-in Types

These 25 types already have parsers. You can override any of them by registering your own parser for the same type:

`AbsolutePath`, `bool`, `byte`, `char`, `DateOnly`, `DateTime`, `DateTimeOffset`, `decimal`, `DirectoryInfo`, `double`, `FileInfo`, `float`, `Guid`, `int`, `long`, `sbyte`, `short`, `string`, `TimeOnly`, `TimeSpan`, `uint`, `ulong`, `Uri`, `ushort`, `Version`

## Repeatable Options and Multi-Value Arguments

Repeat an option to fill a collection. Supported shapes are `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, and `IList<T>`. Except for `object`, the element type must have a parser registered for it: the built-in types already have one, and enums are not built-ins, so `List<MyEnum>` needs a parser registered for `MyEnum` first. See [Limiting Values With `FromAmong`](#limiting-values-with-fromamong) for scalar enum help. For example:

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

When the value is not allowed, the error names the list: `Must be one of: ...`. The same rule applies to positional arguments. Plain scalar enum options and arguments fill their allowed list from the enum names automatically; this does not extend to collection elements. A matched string value binds verbatim: `--mode=JSON` binds as `JSON` when `FromAmong` lists `json` (matching may ignore case unless `CaseSensitive` is set, delivery keeps the typed text). `FromAmong` also gates parser defaults: a default outside the list is ignored, while explicit invalid values still error.
