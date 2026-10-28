using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Collections;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process tests for applying <see cref="ICommandTypeParser.GetDefaultValue"/>
/// to omitted optional valued options and showing the same default in help.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class OptionParserDefaultTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const string PortVariable = "PARKER_PARSERDEFAULT_PORT";
    private const string ControlPayload =
        "\u001b[2J\u001b]0;pwned\u0007\u007f\u0085\u009b\r\n\u2028\u2029spoof";

    public readonly struct Port
    {
        public Port(int value) => Value = value;
        public int Value { get; }
    }

    public sealed class Token
    {
        public Token(string value) => Value = value;
        public string Value { get; }
    }

    public readonly struct Zero
    {
    }

    public sealed class PortParser : ICommandTypeParser
    {
        public Type Type => typeof(Port);

        public object? Parse(string? value, out string? validateError)
        {
            if (int.TryParse(value, out var parsed))
            {
                validateError = null;
                return new Port(parsed);
            }

            validateError = "Port must be an integer.";
            return null;
        }

        public string? GetString(object? value) => value is Port port ? $"P{port.Value}" : value?.ToString();

        public object? GetDefaultValue() => new Port(7000);

        public Array CreateTypedArray(int length) => new Port[length];

        public IList CreateTypedList(int capacity) => new List<Port>(capacity);
    }

    public sealed class TokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => value is Token token ? token.Value : value?.ToString();

        public object? GetDefaultValue() => new Token("class-default");

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class NullTokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => value is Token token ? token.Value : value?.ToString();

        public object? GetDefaultValue() => null;

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class ZeroParser : ICommandTypeParser
    {
        public Type Type => typeof(Zero);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return default(Zero);
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => default(Zero);

        public Array CreateTypedArray(int length) => new Zero[length];

        public IList CreateTypedList(int capacity) => new List<Zero>(capacity);
    }

    public sealed class ThrowingDefaultParser : ICommandTypeParser
    {
        public Type Type => typeof(Zero);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return default(Zero);
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => throw new InvalidOperationException("No parser default.");

        public Array CreateTypedArray(int length) => new Zero[length];

        public IList CreateTypedList(int capacity) => new List<Zero>(capacity);
    }

    [Command("parserdefault", "Probes parser defaults for omitted options.")]
    public sealed class ParserDefaultCommand : Command
    {
        [CommandOption("port", Description = "Port value.", EnvironmentVariable = PortVariable)]
        public Port Port { get; set; }

        [CommandOption("init-port", Description = "Port with an initializer.")]
        public Port InitPort { get; set; } = new(1234);

        [CommandOption("token", Description = "Token value.")]
        public Token? Token { get; set; }

        [CommandOption("flag", Description = "Flag value.")]
        public bool Flag { get; set; }

        [CommandOption("tags", Description = "Tag values.")]
        public string[]? Tags { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"port={Port.Value} init-port={InitPort.Value} token={Token?.Value ?? "null"} flag={Flag} tags={(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("reqparser", "Probes required options with a parser default.")]
    public sealed class RequiredPortCommand : Command
    {
        [CommandOption("port", Description = "Required port.", Required = true)]
        public Port Port { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("zeroparser", "Probes CLR-default parser defaults.")]
    public sealed class ZeroCommand : Command
    {
        [CommandOption("zero", Description = "Zero value.")]
        public Zero Zero { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("zero ran");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class NullablePortParser : ICommandTypeParser
    {
        public Type Type => typeof(Port?);

        public object? Parse(string? value, out string? validateError)
        {
            if (value is null)
            {
                validateError = null;
                return null;
            }

            if (int.TryParse(value, out var parsed))
            {
                validateError = null;
                return new Port(parsed);
            }

            validateError = "Port must be an integer.";
            return null;
        }

        public string? GetString(object? value) => value is Port port ? $"NP{port.Value}" : value?.ToString();

        public object? GetDefaultValue() => new Port(9000);

        public Array CreateTypedArray(int length) => new Port?[length];

        public IList CreateTypedList(int capacity) => new List<Port?>(capacity);
    }

    public sealed class WrongTypePortParser : ICommandTypeParser
    {
        public Type Type => typeof(Port);

        public object? Parse(string? value, out string? validateError)
        {
            if (int.TryParse(value, out var parsed))
            {
                validateError = null;
                return new Port(parsed);
            }

            validateError = "Port must be an integer.";
            return null;
        }

        public string? GetString(object? value) => value is Port port ? $"P{port.Value}" : value?.ToString();

        public object? GetDefaultValue() => "not-a-port";

        public Array CreateTypedArray(int length) => new Port[length];

        public IList CreateTypedList(int capacity) => new List<Port>(capacity);
    }

    public sealed class WrongTypeStringParser : ICommandTypeParser
    {
        public Type Type => typeof(string);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => 42;

        public Array CreateTypedArray(int length) => new string[length];

        public IList CreateTypedList(int capacity) => new List<string>(capacity);
    }

    public sealed class NullableIntParser : ICommandTypeParser
    {
        public Type Type => typeof(int?);

        public object? Parse(string? value, out string? validateError)
        {
            if (int.TryParse(value, out var parsed))
            {
                validateError = null;
                return parsed;
            }

            validateError = "Value must be an integer.";
            return null;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => 42;

        public Array CreateTypedArray(int length) => new int?[length];

        public IList CreateTypedList(int capacity) => new List<int?>(capacity);
    }

    public sealed class NullableBoolParser : ICommandTypeParser
    {
        public Type Type => typeof(bool?);

        public object? Parse(string? value, out string? validateError)
        {
            if (bool.TryParse(value, out var parsed))
            {
                validateError = null;
                return parsed;
            }

            validateError = "Value must be a boolean.";
            return null;
        }

        public string? GetString(object? value) => value is bool flag ? (flag ? "true" : "false") : value?.ToString();

        public object? GetDefaultValue() => true;

        public Array CreateTypedArray(int length) => new bool?[length];

        public IList CreateTypedList(int capacity) => new List<bool?>(capacity);
    }

    public sealed class BoolTrueParser : ICommandTypeParser
    {
        public Type Type => typeof(bool);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            if (string.IsNullOrEmpty(value))
                return true;
            if (value == "1")
                return true;
            if (value == "0")
                return false;
            return bool.TryParse(value, out var parsed) ? parsed : true;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => true;

        public Array CreateTypedArray(int length) => new bool[length];

        public IList CreateTypedList(int capacity) => new List<bool>(capacity);
    }

    public sealed class LegacyStringParser : ICommandTypeParser
    {
        public Type Type => typeof(string);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => "legacy";

        public Array CreateTypedArray(int length) => new string[length];

        public IList CreateTypedList(int capacity) => new List<string>(capacity);
    }

    public sealed class NullFormatTokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => null;

        public object? GetDefaultValue() => new Token("null-format");

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class EmptyFormatTokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => string.Empty;

        public object? GetDefaultValue() => new Token("empty-format");

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class ThrowingFormatTokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => throw new InvalidOperationException("Unformattable.");

        public object? GetDefaultValue() => new Token("throw-format");

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class ControlPayloadTokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => ControlPayload;

        public object? GetDefaultValue() => new Token("control-format");

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class ControlOnlyPayloadTokenParser : ICommandTypeParser
    {
        public Type Type => typeof(Token);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Token(value);
        }

        public string? GetString(object? value) => "\u001b\u001b\u0007\u0085\u009b\r\n\u2028\u2029 \t";

        public object? GetDefaultValue() => new Token("control-only-format");

        public Array CreateTypedArray(int length) => new Token[length];

        public IList CreateTypedList(int capacity) => new List<Token>(capacity);
    }

    public sealed class Bag
    {
        public Bag(string name) => Name = name;

        public string Name { get; set; }
    }

    public sealed class BagParser : ICommandTypeParser
    {
        private int _calls;

        public Type Type => typeof(Bag);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value is null ? null : new Bag(value);
        }

        public string? GetString(object? value) => (value as Bag)?.Name;

        public object? GetDefaultValue() => new Bag($"bag-{++_calls}");

        public Array CreateTypedArray(int length) => new Bag[length];

        public IList CreateTypedList(int capacity) => new List<Bag>(capacity);
    }

    public abstract class InheritedPortBase : Command
    {
        [CommandOption("port", Description = "Inherited port.")]
        public Port Port { get; set; }
    }

    [Command("inherited alpha", "First inherited-option leaf.")]
    public sealed class InheritedPortAlphaCommand : InheritedPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"inherited alpha:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("inherited beta", "Second inherited-option leaf.")]
    public sealed class InheritedPortBetaCommand : InheritedPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"inherited beta:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class InheritedInitPortBase : Command
    {
        [CommandOption("port", Description = "Inherited initialized port.")]
        public Port Port { get; set; } = new(1234);
    }

    [Command("inheritedinit alpha", "First inherited-initializer leaf.")]
    public sealed class InheritedInitPortAlphaCommand : InheritedInitPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"inheritedinit alpha:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("inheritedinit beta", "Second inherited-initializer leaf.")]
    public sealed class InheritedInitPortBetaCommand : InheritedInitPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"inheritedinit beta:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class ShadowedTextBase : Command
    {
        [CommandOption("value", Description = "Shadowed text.")]
        public string? Value { get; set; } = "base-default";
    }

    [Command("shadowedtext", "Derived shadow with an initializer.")]
    public sealed class ShadowedTextCommand : ShadowedTextBase
    {
        public new string? Value { get; set; } = "derived-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"shadowedtext:base={base.Value ?? "null"}|derived={Value ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class ShadowedPortBase : Command
    {
        [CommandOption("port", Description = "Shadowed port.")]
        public Port Port { get; set; }
    }

    [Command("shadowedport", "Derived shadow with a parser default.")]
    public sealed class ShadowedPortCommand : ShadowedPortBase
    {
        public new Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"shadowedport:base={base.Port.Value}|derived={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class HiddenAttributeInitPortBase : Command
    {
        [CommandOption("port", Description = "Base hidden-attribute port.")]
        public Port Port { get; set; } = new(1234);
    }

    [Command("hiddenattrinit", "Hidden attribute shadow with an initializer.")]
    public sealed class HiddenAttributeInitPortCommand : HiddenAttributeInitPortBase
    {
        [CommandOption("derived-port", Description = "Derived hidden-attribute port.")]
        public new Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"hiddenattrinit:base={base.Port.Value}|derived={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class HiddenAttributeParserPortBase : Command
    {
        [CommandOption("port", Description = "Base hidden-attribute parser port.")]
        public Port Port { get; set; }
    }

    [Command("hiddenattrparser", "Hidden attribute shadow with a parser default.")]
    public sealed class HiddenAttributeParserPortCommand : HiddenAttributeParserPortBase
    {
        [CommandOption("derived-port", Description = "Derived hidden-attribute parser port.")]
        public new Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"hiddenattrparser:base={base.Port.Value}|derived={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class HiddenAttributeSameTermParserPortBase : Command
    {
        [CommandOption("port", Description = "Base same-term hidden-attribute parser port.")]
        public Port Port { get; set; }
    }

    [Command("hiddenattrsameparser", "Same-term hidden attribute shadow with a parser default.")]
    public sealed class HiddenAttributeSameTermParserPortCommand : HiddenAttributeSameTermParserPortBase
    {
        [CommandOption("port", Description = "Derived same-term hidden-attribute parser port.")]
        public new Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"hiddenattrsameparser:base={base.Port.Value}|derived={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class PrivateShadowInitPortBase : Command
    {
        [CommandOption("port", Description = "Private base initialized port with a public shadow.")]
        private Port Port { get; set; } = new(1234);

        protected int CurrentPort => Port.Value;
    }

    [Command("privateshadowinit", "Private base initializer with a public shadow.")]
    public sealed class PrivateShadowInitPortCommand : PrivateShadowInitPortBase
    {
        public Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"privateshadowinit:base={CurrentPort}|shadow={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class PrivateShadowParserPortBase : Command
    {
        [CommandOption("port", Description = "Private base parser port with a public shadow.")]
        private Port Port { get; set; }

        protected int CurrentPort => Port.Value;
    }

    [Command("privateshadowparser", "Private base parser default with a public shadow.")]
    public sealed class PrivateShadowParserPortCommand : PrivateShadowParserPortBase
    {
        public Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"privateshadowparser:base={CurrentPort}|shadow={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class OverridePortBase : Command
    {
        [CommandOption("port", Description = "Override port.")]
        public virtual Port Port { get; set; } = new(1234);
    }

    [Command("overrideport", "Overridden port option.")]
    public sealed class OverridePortCommand : OverridePortBase
    {
        public override Port Port { get; set; } = new(999);

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"overrideport:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class IntermediateOverrideInitPortBase : Command
    {
        [CommandOption("port", Description = "Base intermediate override port.")]
        public virtual Port Port { get; set; } = new(1234);
    }

    public abstract class IntermediateOverrideInitPortMid : IntermediateOverrideInitPortBase
    {
        [CommandOption("port", Description = "Intermediate override port.")]
        public override Port Port { get; set; } = new(2345);
    }

    [Command("intermediateoverrideinit", "Intermediate override with an initializer.")]
    public sealed class IntermediateOverrideInitPortLeaf : IntermediateOverrideInitPortMid
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"intermediateoverrideinit:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class IntermediateOverrideParserPortBase : Command
    {
        [CommandOption("port", Description = "Base intermediate parser port.")]
        public virtual Port Port { get; set; }
    }

    public abstract class IntermediateOverrideParserPortMid : IntermediateOverrideParserPortBase
    {
        [CommandOption("port", Description = "Intermediate parser port.")]
        public override Port Port { get; set; }
    }

    [Command("intermediateoverrideparser", "Intermediate override with a parser default.")]
    public sealed class IntermediateOverrideParserPortLeaf : IntermediateOverrideParserPortMid
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"intermediateoverrideparser:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class ProtectedPortBase : Command
    {
        [CommandOption("port", Description = "Protected inherited port.")]
        protected Port Port { get; set; } = new(1234);

        protected int CurrentPort => Port.Value;
    }

    [Command("protectedport", "Protected inherited option.")]
    public sealed class ProtectedPortCommand : ProtectedPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"protectedport:port={CurrentPort}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class PrivateInitPortBase : Command
    {
        [CommandOption("port", Description = "Private base initialized port.")]
        private Port Port { get; set; } = new(1234);

        protected int CurrentPort => Port.Value;
    }

    [Command("privateinitport", "Private base option with an initializer.")]
    public sealed class PrivateInitPortCommand : PrivateInitPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"privateinitport:port={CurrentPort}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class PrivateParserPortBase : Command
    {
        [CommandOption("port", Description = "Private base parser port.")]
        private Port Port { get; set; }

        protected int CurrentPort => Port.Value;
    }

    [Command("privateparserport", "Private base option with a parser default.")]
    public sealed class PrivateParserPortCommand : PrivateParserPortBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"privateparserport:port={CurrentPort}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("nullableport", "Probes nullable options with parser defaults.")]
    public sealed class NullablePortCommand : Command
    {
        [CommandOption("port", Description = "Nullable port.")]
        public Port? Port { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"nullableport:port={(Port.HasValue ? Port.Value.Value.ToString() : "null")}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("wrongtypeport", "Probes a wrong-typed parser default.")]
    public sealed class WrongTypePortCommand : Command
    {
        [CommandOption("port", Description = "Port value.")]
        public Port Port { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"wrongtypeport:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("wrongtypefromamong", "Probes a wrong-typed parser default against allowed values.")]
    public sealed class WrongTypeFromAmongCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["42"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"wrongtypefromamong:mode={Mode ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("nullableinitvalues", "Probes nullable value initializers.")]
    public sealed class NullableValueInitializerCommand : Command
    {
        [CommandOption("port", Description = "Nullable port.")]
        public int? Port { get; set; } = 0;

        [CommandOption("enabled", Description = "Nullable enabled.")]
        public bool? Enabled { get; set; } = false;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"nullableinitvalues:port={(Port.HasValue ? Port.Value.ToString() : "null")}|enabled={(Enabled.HasValue ? (Enabled.Value ? "true" : "false") : "null")}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("nullableinitnull", "Probes a null nullable initializer.")]
    public sealed class NullableNullInitializerCommand : Command
    {
        [CommandOption("port", Description = "Nullable port.")]
        public int? Port { get; set; } = null;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"nullableinitnull:port={(Port.HasValue ? Port.Value.ToString() : "null")}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("stringinit", "Probes reference initializers.")]
    public sealed class StringInitializerCommand : Command
    {
        [CommandOption("name", Description = "Nullable name.")]
        public string? Name { get; set; } = null;

        [CommandOption("fixed-name", Description = "Fixed name.")]
        public string? FixedName { get; set; } = "fixed";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"stringinit:name={Name ?? "null"}|fixed-name={FixedName ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("flagprobe", "Probes flags with a non-noop parser default.")]
    public sealed class FlagOnlyCommand : Command
    {
        [CommandOption("flag", Description = "Flag value.")]
        public bool Flag { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"flag={Flag}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("tokencollection", "Probes collections with a non-noop parser default.")]
    public sealed class TokenCollectionCommand : Command
    {
        [CommandOption("tokens", Description = "Token values.")]
        public Token[]? Tokens { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"tokens={(Tokens is null ? "null" : string.Join(",", Tokens.Select(token => token.Value)))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("requiredtoken", "Probes required options with a non-noop parser default.")]
    public sealed class RequiredTokenCommand : Command
    {
        [CommandOption("token", Description = "Required token.", Required = true)]
        public Token Token { get; set; } = null!;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"token={Token.Value}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("secretport", "Probes secret parser defaults.")]
    public sealed class SecretPortCommand : Command
    {
        [CommandOption("port", Description = "Secret port.", Secret = true)]
        public Port Port { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"secretport:port={Port.Value}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("unreadableport", "Probes unreadable option getters.")]
    public sealed class UnreadablePortCommand : Command
    {
        [CommandOption("port", Description = "Unreadable port.")]
        public Port Port
        {
            get => throw new InvalidOperationException("Unreadable port.");
            set { }
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("unreadableport ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("fromamong", "Probes parser defaults against allowed values.")]
    public sealed class FromAmongStringCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["safe"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"fromamong:mode={Mode ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("fromamongallowed", "Probes an allowed parser default.")]
    public sealed class FromAmongAllowedStringCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["legacy", "safe"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"fromamongallowed:mode={Mode ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("tokenformat", "Probes parser default formatting.")]
    public sealed class TokenFormatCommand : Command
    {
        [CommandOption("token", Description = "Token value.")]
        public Token? Token { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"tokenformat:token={Token?.Value ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("bags", "Probes per-option parser defaults of one mutable type.")]
    public sealed class BagsCommand : Command
    {
        [CommandOption("first", Description = "First bag.")]
        public Bag? First { get; set; }

        [CommandOption("second", Description = "Second bag.")]
        public Bag? Second { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            First!.Name += "-mutated";
            Console.WriteLine($"first={First.Name}|second={Second!.Name}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("emptyinit", "Probes an empty-string initializer.")]
    public sealed class EmptyStringInitCommand : Command
    {
        [CommandOption("text", Description = "Text value.")]
        public string? Text { get; set; } = "";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"emptyinit:text-length={Text?.Length ?? -1}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("helpinject", "Probes help default sanitization.")]
    public sealed class HelpTextInjectionCommand : Command
    {
        [CommandOption("text", Description = "Text value.")]
        public string? Text { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"helpinject:text-length={Text?.Length ?? 0}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ControlToString
    {
        public override string ToString() => ControlPayload;
    }

    [Command("controltostring", "Probes control characters in ToString fallbacks.")]
    public sealed class ControlToStringCommand : Command
    {
        [CommandOption("value", Description = "Value with a control-bearing ToString.")]
        public ControlToString? Value { get; set; } = new();

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("controltostring ran");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ThrowingToString
    {
        public override string ToString() => throw new InvalidOperationException("No display form.");
    }

    [Command("throwntostring", "Probes throwing ToString fallbacks.")]
    public sealed class ThrowingToStringCommand : Command
    {
        [CommandOption("value", Description = "Value with a throwing ToString.")]
        public ThrowingToString? Value { get; set; } = new();

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("throwntostring ran");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SharedTextBase : Command
    {
        [CommandOption("text", Description = "Shared text.")]
        public string? Text { get; set; }
    }

    [Command("text alpha", "First shared-text leaf.")]
    public sealed class SharedTextAlphaCommand : SharedTextBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"text alpha:length={Text?.Length ?? 0}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("text beta", "Second shared-text leaf.")]
    public sealed class SharedTextBetaCommand : SharedTextBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"text beta:length={Text?.Length ?? 0}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task OmittedStructOption_BindsParserDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder().AddCommand<ParserDefaultCommand>(), ["parserdefault"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("port=7000", output);
        Assert.Contains("init-port=1234", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ExplicitValues_WinOverParserDefaultAndInitializer()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder().AddCommand<ParserDefaultCommand>(),
            ["parserdefault", "--port=42", "--init-port=99"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("port=42", output);
        Assert.Contains("init-port=99", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EnvironmentValue_WinsOverParserDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder().AddCommand<ParserDefaultCommand>(),
            ["parserdefault"],
            new Dictionary<string, string?> { [PortVariable] = "77" });

        Assert.Equal(0, exitCode);
        Assert.Contains("port=77", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task OmittedClassOption_BindsParserDefault()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<TokenParser>()
            .AddCommand<ParserDefaultCommand>();

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["parserdefault"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("token=class-default", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NullClassParserDefault_LeavesPropertyNull()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<NullTokenParser>()
            .AddCommand<ParserDefaultCommand>();

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["parserdefault"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("token=null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Help_ShowsParserDefaultAndInitializerWithParserFormatting()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder().AddCommand<ParserDefaultCommand>(),
            ["parserdefault", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Default: P7000", output);
        Assert.Contains("Default: P1234", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FlagsAndCollections_AreNotFilledFromParserDefaults()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder().AddCommand<ParserDefaultCommand>(),
            ["parserdefault"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("flag=False", output);
        Assert.Contains("tags=null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task RequiredOption_IsNotSatisfiedByParserDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder().AddCommand<RequiredPortCommand>(),
            ["reqparser"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Missing required option", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task RequiredOption_HelpOmitsParserDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder().AddCommand<RequiredPortCommand>(),
            ["reqparser", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("(required)", output);
        Assert.DoesNotContain("Default:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ClrDefaultParserDefault_LeavesValueAndHelpUnchanged()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<ZeroParser>()
            .AddCommand<ZeroCommand>();

        var (exitCode, _, error) = await RunCapturedAsync(builder, ["zeroparser"]);
        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var (helpCode, helpOutput, helpError) = await RunCapturedAsync(builder, ["zeroparser", "--help"]);
        Assert.Equal(0, helpCode);
        Assert.DoesNotContain("Default:", helpOutput);
        Assert.True(string.IsNullOrWhiteSpace(helpError), $"Expected empty stderr but got: {helpError}");
    }

    [Fact]
    public async Task ThrowingParserDefault_IsIgnored()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<ThrowingDefaultParser>()
            .AddCommand<ZeroCommand>();

        var (exitCode, _, error) = await RunCapturedAsync(builder, ["zeroparser"]);
        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var (helpCode, helpOutput, helpError) = await RunCapturedAsync(builder, ["zeroparser", "--help"]);
        Assert.Equal(0, helpCode);
        Assert.DoesNotContain("Default:", helpOutput);
        Assert.True(string.IsNullOrWhiteSpace(helpError), $"Expected empty stderr but got: {helpError}");
    }

    [Fact]
    public async Task InstanceRegistration_DoesNotReapplyParserDefaultAfterMutation()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<PortParser>();
        builder.AddCommand(new ParserDefaultCommand());

        var first = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, first.ExitCode);
        Assert.Contains("port=7000", first.Output);

        var explicitRun = await RunCapturedAsync(builder, ["parserdefault", "--port=42"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("port=42", explicitRun.Output);

        var second = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("port=42", second.Output);
    }

    [Fact]
    public async Task InstanceRegistration_ExternalChange_KeepsLiveValueForBindingAndHelp()
    {
        var command = new ParserDefaultCommand();
        var builder = CreateBuilder();
        builder.AddCommand(command);
        command.Port = new Port(55);

        var run = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("port=55", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["parserdefault", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P55", help.Output);
        Assert.DoesNotContain("Default: P7000", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_ExplicitBindResetToSnapshot_DoesNotReapplyParserDefault()
    {
        var command = new ParserDefaultCommand();
        var builder = CreateBuilder();
        builder.AddCommand(command);

        var explicitRun = await RunCapturedAsync(builder, ["parserdefault", "--port=42"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("port=42", explicitRun.Output);

        command.Port = default;

        var omittedRun = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, omittedRun.ExitCode);
        Assert.Contains("port=0", omittedRun.Output);
        Assert.DoesNotContain("port=7000", omittedRun.Output);

        var help = await RunCapturedAsync(builder, ["parserdefault", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P0", help.Output);
        Assert.DoesNotContain("Default: P7000", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task InheritedInstanceOption_NoInitializer_BindsAndShowsParserDefault()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InheritedPortAlphaCommand());
        builder.AddCommand(new InheritedPortBetaCommand());

        var leafHelp = await RunCapturedAsync(builder, ["inherited", "alpha", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P7000", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P7000", rootHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(rootHelp.Error), $"Expected empty stderr but got: {rootHelp.Error}");

        var run = await RunCapturedAsync(builder, ["inherited", "alpha"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("inherited alpha:port=7000", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InheritedInstanceOption_ExplicitValuesWinAcrossRunsAndHelp()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InheritedPortAlphaCommand());
        builder.AddCommand(new InheritedPortBetaCommand());

        var zeroRun = await RunCapturedAsync(builder, ["inherited", "alpha", "--port=0"]);
        Assert.Equal(0, zeroRun.ExitCode);
        Assert.Contains("inherited alpha:port=0", zeroRun.Output);

        var zeroHelp = await RunCapturedAsync(builder, ["inherited", "alpha", "--help"]);
        Assert.Equal(0, zeroHelp.ExitCode);
        Assert.Contains("Default: P0", zeroHelp.Output);
        Assert.DoesNotContain("Default: P7000", zeroHelp.Output);

        var zeroOmittedRun = await RunCapturedAsync(builder, ["inherited", "alpha"]);
        Assert.Equal(0, zeroOmittedRun.ExitCode);
        Assert.Contains("inherited alpha:port=0", zeroOmittedRun.Output);

        var explicitRun = await RunCapturedAsync(builder, ["inherited", "alpha", "--port=42"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("inherited alpha:port=42", explicitRun.Output);

        var explicitHelp = await RunCapturedAsync(builder, ["inherited", "alpha", "--help"]);
        Assert.Equal(0, explicitHelp.ExitCode);
        Assert.Contains("Default: P42", explicitHelp.Output);
    }

    [Fact]
    public async Task InheritedInstanceOption_WithInitializer_KeepsInitializerInBindingAndHelp()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InheritedInitPortAlphaCommand());
        builder.AddCommand(new InheritedInitPortBetaCommand());

        var leafHelp = await RunCapturedAsync(builder, ["inheritedinit", "alpha", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P1234", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P1234", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["inheritedinit", "alpha"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("inheritedinit alpha:port=1234", run.Output);

        var explicitRun = await RunCapturedAsync(builder, ["inheritedinit", "alpha", "--port=42"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("inheritedinit alpha:port=42", explicitRun.Output);

        var secondHelp = await RunCapturedAsync(builder, ["inheritedinit", "alpha", "--help"]);
        Assert.Equal(0, secondHelp.ExitCode);
        Assert.Contains("Default: P1234", secondHelp.Output);
        Assert.DoesNotContain("Default: P42", secondHelp.Output);

        var secondRun = await RunCapturedAsync(builder, ["inheritedinit", "alpha"]);
        Assert.Equal(0, secondRun.ExitCode);
        Assert.Contains("inheritedinit alpha:port=42", secondRun.Output);
    }

    [Fact]
    public async Task SingleLeafInheritedInstanceInitializer_PromotesAndAgrees()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InheritedInitPortAlphaCommand());

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P1234", rootHelp.Output);

        var leafHelp = await RunCapturedAsync(builder, ["inheritedinit", "alpha", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P1234", leafHelp.Output);

        var run = await RunCapturedAsync(builder, ["inheritedinit", "alpha"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("inheritedinit alpha:port=1234", run.Output);
    }

    [Fact]
    public async Task SingleLeafInheritedInstanceParserDefault_PromotesAndAgrees()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InheritedPortAlphaCommand());

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P7000", rootHelp.Output);

        var leafHelp = await RunCapturedAsync(builder, ["inherited", "alpha", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P7000", leafHelp.Output);

        var run = await RunCapturedAsync(builder, ["inherited", "alpha"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("inherited alpha:port=7000", run.Output);
    }

    [Fact]
    public async Task InstanceRegistration_DerivedShadow_InitializerWinsForBindingAndHelp()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<LegacyStringParser>();
        builder.AddCommand(new ShadowedTextCommand());

        var leafHelp = await RunCapturedAsync(builder, ["shadowedtext", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: base-default", leafHelp.Output);
        Assert.DoesNotContain("Default: derived-default", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--value", rootHelp.Output);
        Assert.Contains("Default: base-default", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["shadowedtext"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("shadowedtext:base=base-default|derived=derived-default", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_DerivedShadow_ParserDefaultBindsBaseOptionAndShowsInHelp()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new ShadowedPortCommand());

        var leafHelp = await RunCapturedAsync(builder, ["shadowedport", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P7000", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P7000", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["shadowedport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("shadowedport:base=7000|derived=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_HiddenAttributeShadow_InitializerWinsForBaseHelpAndBinding()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new HiddenAttributeInitPortCommand());

        var leafHelp = await RunCapturedAsync(builder, ["hiddenattrinit", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("--port", leafHelp.Output);
        Assert.Contains("--derived-port", leafHelp.Output);
        Assert.Contains("Default: P1234", leafHelp.Output);
        Assert.Contains("Default: P999", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P1234", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["hiddenattrinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("hiddenattrinit:base=1234|derived=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_HiddenAttributeShadow_ParserDefaultBindsBaseOption()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new HiddenAttributeParserPortCommand());

        var leafHelp = await RunCapturedAsync(builder, ["hiddenattrparser", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("--port", leafHelp.Output);
        Assert.Contains("--derived-port", leafHelp.Output);
        Assert.Contains("Default: P7000", leafHelp.Output);
        Assert.Contains("Default: P999", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P7000", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["hiddenattrparser"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("hiddenattrparser:base=7000|derived=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var derivedRun = await RunCapturedAsync(builder, ["hiddenattrparser", "--derived-port=43"]);
        Assert.Equal(0, derivedRun.ExitCode);
        Assert.Contains("hiddenattrparser:base=7000|derived=43", derivedRun.Output);
        Assert.True(string.IsNullOrWhiteSpace(derivedRun.Error), $"Expected empty stderr but got: {derivedRun.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_HiddenAttributeSameTerm_ParserDefaultBindsBaseOption()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new HiddenAttributeSameTermParserPortCommand());

        var help = await RunCapturedAsync(builder, ["hiddenattrsameparser", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P7000", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["hiddenattrsameparser"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("hiddenattrsameparser:base=7000|derived=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_HiddenAttributeShadow_InitializerWinsForBaseHelpAndBinding()
    {
        var builder = CreateBuilder()
            .AddCommand<HiddenAttributeInitPortCommand>();

        var leafHelp = await RunCapturedAsync(builder, ["hiddenattrinit", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P1234", leafHelp.Output);
        Assert.Contains("Default: P999", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P1234", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["hiddenattrinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("hiddenattrinit:base=1234|derived=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_HiddenAttributeShadow_ParserDefaultBindsBaseOption()
    {
        var builder = CreateBuilder()
            .AddCommand<HiddenAttributeParserPortCommand>();

        var leafHelp = await RunCapturedAsync(builder, ["hiddenattrparser", "--help"]);
        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("Default: P7000", leafHelp.Output);
        Assert.Contains("Default: P999", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P7000", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["hiddenattrparser"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("hiddenattrparser:base=7000|derived=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_PrivateBaseInitializerWithPublicShadow_KeepsHelpAndBindingValue()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new PrivateShadowInitPortCommand());

        var help = await RunCapturedAsync(builder, ["privateshadowinit", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P1234", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["privateshadowinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("privateshadowinit:base=1234|shadow=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_PrivateBaseParserDefaultWithPublicShadow_FailsClosed()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new PrivateShadowParserPortCommand());

        var help = await RunCapturedAsync(builder, ["privateshadowparser", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["privateshadowparser"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("privateshadowparser:base=0|shadow=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_OverrideProperty_UsesOverrideValueForHelpAndBinding()
    {
        var builder = CreateBuilder()
            .AddCommand<OverridePortCommand>();

        var help = await RunCapturedAsync(builder, ["overrideport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P999", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var rootHelp = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("--port", rootHelp.Output);
        Assert.Contains("Default: P999", rootHelp.Output);

        var run = await RunCapturedAsync(builder, ["overrideport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("overrideport:port=999", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_IntermediateOverride_KeepsInitializerForHelpAndBinding()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new IntermediateOverrideInitPortLeaf());

        var help = await RunCapturedAsync(builder, ["intermediateoverrideinit", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P2345", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["intermediateoverrideinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("intermediateoverrideinit:port=2345", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_IntermediateOverride_BindsParserDefault()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new IntermediateOverrideParserPortLeaf());

        var help = await RunCapturedAsync(builder, ["intermediateoverrideparser", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P7000", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["intermediateoverrideparser"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("intermediateoverrideparser:port=7000", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_IntermediateOverride_ExplicitBindResetToSnapshot_DoesNotReapplyParserDefault()
    {
        var command = new IntermediateOverrideParserPortLeaf();
        var builder = CreateBuilder();
        builder.AddCommand(command);

        var explicitRun = await RunCapturedAsync(builder, ["intermediateoverrideparser", "--port=42"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("intermediateoverrideparser:port=42", explicitRun.Output);
        Assert.True(string.IsNullOrWhiteSpace(explicitRun.Error), $"Expected empty stderr but got: {explicitRun.Error}");

        command.Port = default;

        var omittedRun = await RunCapturedAsync(builder, ["intermediateoverrideparser"]);
        Assert.Equal(0, omittedRun.ExitCode);
        Assert.Contains("intermediateoverrideparser:port=0", omittedRun.Output);
        Assert.DoesNotContain("intermediateoverrideparser:port=7000", omittedRun.Output);
        Assert.True(string.IsNullOrWhiteSpace(omittedRun.Error), $"Expected empty stderr but got: {omittedRun.Error}");
    }

    [Fact]
    public async Task TypeRegistration_IntermediateOverride_KeepsInitializerForHelpAndBinding()
    {
        var builder = CreateBuilder()
            .AddCommand<IntermediateOverrideInitPortLeaf>();

        var help = await RunCapturedAsync(builder, ["intermediateoverrideinit", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P2345", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["intermediateoverrideinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("intermediateoverrideinit:port=2345", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_IntermediateOverride_BindsParserDefault()
    {
        var builder = CreateBuilder()
            .AddCommand<IntermediateOverrideParserPortLeaf>();

        var help = await RunCapturedAsync(builder, ["intermediateoverrideparser", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P7000", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["intermediateoverrideparser"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("intermediateoverrideparser:port=7000", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_ProtectedInheritedOption_KeepsInitializerForBindingAndHelp()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new ProtectedPortCommand());

        var help = await RunCapturedAsync(builder, ["protectedport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P1234", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["protectedport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("protectedport:port=1234", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_ProtectedInheritedOption_KeepsInitializerForBindingAndHelp()
    {
        var builder = CreateBuilder()
            .AddCommand<ProtectedPortCommand>();

        var help = await RunCapturedAsync(builder, ["protectedport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P1234", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["protectedport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("protectedport:port=1234", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_PrivateBaseInitializer_HelpKeepsInitializerAndBindingKeepsValue()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new PrivateInitPortCommand());

        var help = await RunCapturedAsync(builder, ["privateinitport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P1234", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["privateinitport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("privateinitport:port=1234", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_PrivateBaseInitializer_HelpKeepsInitializerAndBindingKeepsValue()
    {
        var builder = CreateBuilder()
            .AddCommand<PrivateInitPortCommand>();

        var help = await RunCapturedAsync(builder, ["privateinitport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P1234", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["privateinitport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("privateinitport:port=1234", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task PrivateBaseParserDefault_FailsClosedForBindingAndHelp()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new PrivateParserPortCommand());

        var help = await RunCapturedAsync(builder, ["privateparserport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["privateparserport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("privateparserport:port=0", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegistration_PrivateParserDefault_FailsClosedForBindingAndHelp()
    {
        var builder = CreateBuilder()
            .AddCommand<PrivateParserPortCommand>();

        var help = await RunCapturedAsync(builder, ["privateparserport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["privateparserport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("privateparserport:port=0", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_ExplicitValueThenHelpAndOmittedRunAgree()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new ParserDefaultCommand());

        var explicitRun = await RunCapturedAsync(builder, ["parserdefault", "--port=42"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("port=42", explicitRun.Output);

        var help = await RunCapturedAsync(builder, ["parserdefault", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P42", help.Output);
        Assert.DoesNotContain("Default: P7000", help.Output);

        var omittedRun = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, omittedRun.ExitCode);
        Assert.Contains("port=42", omittedRun.Output);
    }

    [Fact]
    public async Task InstanceRegistration_ExplicitClrDefaultThenHelpAndOmittedRunKeepIt()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new ParserDefaultCommand());

        var explicitRun = await RunCapturedAsync(builder, ["parserdefault", "--port=0"]);
        Assert.Equal(0, explicitRun.ExitCode);
        Assert.Contains("port=0", explicitRun.Output);

        var help = await RunCapturedAsync(builder, ["parserdefault", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P0", help.Output);
        Assert.DoesNotContain("Default: P7000", help.Output);

        var omittedRun = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, omittedRun.ExitCode);
        Assert.Contains("port=0", omittedRun.Output);
    }

    [Fact]
    public async Task InstanceRegistration_Untouched_HelpShowsParserDefaultAndBindingAppliesIt()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new ParserDefaultCommand());

        var help = await RunCapturedAsync(builder, ["parserdefault", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: P7000", help.Output);

        var omittedRun = await RunCapturedAsync(builder, ["parserdefault"]);
        Assert.Equal(0, omittedRun.ExitCode);
        Assert.Contains("port=7000", omittedRun.Output);
    }

    [Fact]
    public async Task ParserFormatNull_OmitsDefaultLine()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<NullFormatTokenParser>()
            .AddCommand<TokenFormatCommand>();

        var help = await RunCapturedAsync(builder, ["tokenformat", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["tokenformat"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("tokenformat:token=null-format", run.Output);
    }

    [Fact]
    public async Task ParserFormatEmpty_OmitsDefaultLine()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<EmptyFormatTokenParser>()
            .AddCommand<TokenFormatCommand>();

        var help = await RunCapturedAsync(builder, ["tokenformat", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["tokenformat"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("tokenformat:token=empty-format", run.Output);
    }

    [Fact]
    public async Task EmptyStringInitializer_WinsOverParserDefault()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<LegacyStringParser>()
            .AddCommand<EmptyStringInitCommand>();

        var run = await RunCapturedAsync(builder, ["emptyinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("emptyinit:text-length=0", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["emptyinit", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.DoesNotContain("legacy", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task ParserFormatThrows_HelpDoesNotFaultAndOmitsDefaultLine()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<ThrowingFormatTokenParser>()
            .AddCommand<TokenFormatCommand>();

        var help = await RunCapturedAsync(builder, ["tokenformat", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builder, ["tokenformat"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("tokenformat:token=throw-format", run.Output);
    }

    [Fact]
    public async Task TwoOptionsOfSameParserType_GetIndependentDefaults()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<BagParser>()
            .AddCommand<BagsCommand>();

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["bags"]);

        Assert.Equal(0, exitCode);
        var line = output.Trim();
        var separator = line.IndexOf('|');
        Assert.True(separator > 0, $"Expected two bag values but got: {output}");
        var first = line[..separator];
        var second = line[(separator + 1)..];
        Assert.EndsWith("-mutated", first);
        Assert.NotEqual(first, second);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ParserDefaultOutsideFromAmong_IsRejected()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<LegacyStringParser>()
            .AddCommand<FromAmongStringCommand>();

        var run = await RunCapturedAsync(builder, ["fromamong"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("fromamong:mode=null", run.Output);

        var help = await RunCapturedAsync(builder, ["fromamong", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.DoesNotContain("legacy", help.Output);

        var explicitRun = await RunCapturedAsync(builder, ["fromamong", "--mode=legacy"]);
        Assert.Equal(2, explicitRun.ExitCode);
        Assert.Contains("Must be one of: safe", explicitRun.Error);
    }

    [Fact]
    public async Task ParserDefaultAllowedByFromAmong_IsApplied()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<LegacyStringParser>()
            .AddCommand<FromAmongAllowedStringCommand>();

        var run = await RunCapturedAsync(builder, ["fromamongallowed"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("fromamongallowed:mode=legacy", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["fromamongallowed", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: legacy", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task NullableOption_DoesNotUseUnderlyingTypeParserDefault()
    {
        var builder = CreateBuilder()
            .AddCommand<NullablePortCommand>();

        var run = await RunCapturedAsync(builder, ["nullableport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("nullableport:port=null", run.Output);

        var help = await RunCapturedAsync(builder, ["nullableport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("P7000", help.Output);
    }

    [Fact]
    public async Task NullableOption_UsesExactNullableTypeParserDefault()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<NullablePortParser>()
            .AddCommand<NullablePortCommand>();

        var run = await RunCapturedAsync(builder, ["nullableport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("nullableport:port=9000", run.Output);

        var help = await RunCapturedAsync(builder, ["nullableport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: NP9000", help.Output);
    }

    [Fact]
    public async Task InstanceRegistration_WrongTypedParserDefault_IsIgnored()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<WrongTypePortParser>();
        builder.AddCommand(new WrongTypePortCommand());

        var run = await RunCapturedAsync(builder, ["wrongtypeport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("wrongtypeport:port=0", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["wrongtypeport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task TypeRegistration_WrongTypedParserDefault_IsIgnored()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<WrongTypePortParser>()
            .AddCommand<WrongTypePortCommand>();

        var run = await RunCapturedAsync(builder, ["wrongtypeport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("wrongtypeport:port=0", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["wrongtypeport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task WrongTypedParserDefault_WithFromAmong_IsIgnored()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<WrongTypeStringParser>();
        builder.AddCommand(new WrongTypeFromAmongCommand());

        var run = await RunCapturedAsync(builder, ["wrongtypefromamong"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("wrongtypefromamong:mode=null", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["wrongtypefromamong", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_NullableZeroAndFalseInitializers_WinOverParserDefaults()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<NullableIntParser>()
            .AddCommandTypeParser<NullableBoolParser>();
        builder.AddCommand(new NullableValueInitializerCommand());

        var run = await RunCapturedAsync(builder, ["nullableinitvalues"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("nullableinitvalues:port=0|enabled=false", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["nullableinitvalues", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: 0", help.Output);
        Assert.Contains("Default: false", help.Output);
        Assert.DoesNotContain("Default: 42", help.Output);
        Assert.DoesNotContain("Default: true", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task TypeRegistration_NullableNullInitializer_UsesParserDefault()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<NullableIntParser>()
            .AddCommand<NullableNullInitializerCommand>();

        var run = await RunCapturedAsync(builder, ["nullableinitnull"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("nullableinitnull:port=42", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["nullableinitnull", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: 42", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task TypeRegistration_StringNullInitializer_UsesParserDefaultAndInitializerWins()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<LegacyStringParser>()
            .AddCommand<StringInitializerCommand>();

        var run = await RunCapturedAsync(builder, ["stringinit"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("stringinit:name=legacy|fixed-name=fixed", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["stringinit", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: legacy", help.Output);
        Assert.Contains("Default: fixed", help.Output);
        Assert.DoesNotContain("Default: null", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task FlagWithNonNoopParser_KeepsFlagDefault()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<BoolTrueParser>()
            .AddCommand<FlagOnlyCommand>();

        var run = await RunCapturedAsync(builder, ["flagprobe"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("flag=False", run.Output);

        var help = await RunCapturedAsync(builder, ["flagprobe", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: False", help.Output);
    }

    [Fact]
    public async Task CollectionWithNonNoopParser_IsNotFilled()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<TokenParser>()
            .AddCommand<TokenCollectionCommand>();

        var run = await RunCapturedAsync(builder, ["tokencollection"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("tokens=null", run.Output);
    }

    [Fact]
    public async Task RequiredOptionWithNonNoopParser_StillFails()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<TokenParser>()
            .AddCommand<RequiredTokenCommand>();

        var run = await RunCapturedAsync(builder, ["requiredtoken"]);
        Assert.Equal(2, run.ExitCode);
        Assert.Contains("Missing required option", run.Error);
    }

    [Fact]
    public async Task SecretParserDefault_BindsButHelpStaysMasked()
    {
        var builder = CreateBuilder()
            .AddCommand<SecretPortCommand>();

        var run = await RunCapturedAsync(builder, ["secretport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("secretport:port=7000", run.Output);

        var help = await RunCapturedAsync(builder, ["secretport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Default: [REDACTED]", help.Output);
        Assert.DoesNotContain("P7000", help.Output);
    }

    [Fact]
    public async Task UnreadableGetter_FailsClosedWithoutFault()
    {
        var builder = CreateBuilder()
            .AddCommand<UnreadablePortCommand>();

        var run = await RunCapturedAsync(builder, ["unreadableport"]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("unreadableport ran", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["unreadableport", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task InstanceRegistration_ExplicitControlPayload_IsSanitizedInHelpDefault()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new HelpTextInjectionCommand());

        var run = await RunCapturedAsync(builder, ["helpinject", "--text", ControlPayload]);
        Assert.Equal(0, run.ExitCode);
        Assert.Contains($"helpinject:text-length={ControlPayload.Length}", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");

        var help = await RunCapturedAsync(builder, ["helpinject", "--help"]);
        Assert.Equal(0, help.ExitCode);
        var defaultLine = GetDefaultLine(help.Output);
        Assert.Contains("spoof", defaultLine);
        AssertSanitizedDefault(defaultLine);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task ParserGetStringControlPayload_IsSanitizedInHelpDefault()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<ControlPayloadTokenParser>()
            .AddCommand<TokenFormatCommand>();

        var help = await RunCapturedAsync(builder, ["tokenformat", "--help"]);
        Assert.Equal(0, help.ExitCode);
        var defaultLine = GetDefaultLine(help.Output);
        Assert.Contains("spoof", defaultLine);
        AssertSanitizedDefault(defaultLine);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task ToStringFallbackControlPayload_IsSanitizedInHelpDefault()
    {
        var builder = CreateBuilder().AddCommand<ControlToStringCommand>();

        var help = await RunCapturedAsync(builder, ["controltostring", "--help"]);
        Assert.Equal(0, help.ExitCode);
        var defaultLine = GetDefaultLine(help.Output);
        Assert.Contains("spoof", defaultLine);
        AssertSanitizedDefault(defaultLine);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task PromotedGlobalControlPayload_IsSanitizedInCrossCommandHelp()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new SharedTextAlphaCommand());
        builder.AddCommand(new SharedTextBetaCommand());

        var alphaRun = await RunCapturedAsync(builder, ["text", "alpha", "--text", ControlPayload]);
        Assert.Equal(0, alphaRun.ExitCode);
        Assert.Contains($"text alpha:length={ControlPayload.Length}", alphaRun.Output);

        var help = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("--text", help.Output);
        var defaultLine = GetDefaultLine(help.Output);
        Assert.Contains("spoof", defaultLine);
        AssertSanitizedDefault(defaultLine);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var betaRun = await RunCapturedAsync(builder, ["text", "beta", "--text", ControlPayload]);
        Assert.Equal(0, betaRun.ExitCode);
        Assert.Contains($"text beta:length={ControlPayload.Length}", betaRun.Output);

        var betaHelp = await RunCapturedAsync(builder, ["text", "beta", "--help"]);
        Assert.Equal(0, betaHelp.ExitCode);
        var betaDefaultLine = GetDefaultLine(betaHelp.Output);
        Assert.Contains("spoof", betaDefaultLine);
        AssertSanitizedDefault(betaDefaultLine);
        Assert.True(string.IsNullOrWhiteSpace(betaHelp.Error), $"Expected empty stderr but got: {betaHelp.Error}");
    }

    [Fact]
    public async Task ParserGetStringControlOnlyPayload_OmitsDefaultLine()
    {
        var builder = CreateBuilder()
            .AddCommandTypeParser<ControlOnlyPayloadTokenParser>()
            .AddCommand<TokenFormatCommand>();

        var help = await RunCapturedAsync(builder, ["tokenformat", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task ThrowingToStringFallback_HelpDoesNotFaultAndOmitsDefaultLine()
    {
        var builder = CreateBuilder().AddCommand<ThrowingToStringCommand>();

        var help = await RunCapturedAsync(builder, ["throwntostring", "--help"]);
        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("Default:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    private static string GetDefaultLine(string output)
    {
        var line = Assert.Single(output.Split('\n'), candidate => candidate.Contains("Default:", StringComparison.Ordinal));
        return line.TrimEnd('\r');
    }

    private static void AssertSanitizedDefault(string text)
    {
        foreach (var ch in text)
            Assert.False(char.IsControl(ch), $"Control character U+{(int)ch:X4} reached the help default.");

        Assert.False(text.Contains('\u007f'), "DEL reached the help default.");
        Assert.DoesNotContain("\u2028", text);
        Assert.DoesNotContain("\u2029", text);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("parserdefault-test")
            .SetExecutableTitle("ParserDefault Test")
            .SetExecutableDescription("Parser default verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommandTypeParser<PortParser>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        ApplicationBuilder builder,
        string[] args,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        await ConsoleGate.WaitAsync();
        var priors = new Dictionary<string, string?>();
        try
        {
            priors[PortVariable] = Environment.GetEnvironmentVariable(PortVariable);
            Environment.SetEnvironmentVariable(PortVariable, null);

            if (environment is not null)
            {
                foreach (var (name, value) in environment)
                {
                    if (!priors.ContainsKey(name))
                        priors[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, value);
                }
            }

            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            try
            {
                var exitCode = await builder.RunAsync(args);
                outWriter.Flush();
                errorWriter.Flush();
                return (exitCode, outWriter.ToString(), errorWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
        finally
        {
            foreach (var (name, prior) in priors)
            {
                Environment.SetEnvironmentVariable(name, prior);
            }

            ConsoleGate.Release();
        }
    }
}
