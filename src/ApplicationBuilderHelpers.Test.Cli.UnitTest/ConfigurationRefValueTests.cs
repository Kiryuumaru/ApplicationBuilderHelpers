using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Collections;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Direct in-process unit tests for the public <see cref="ConfigurationExtensions"/>
/// reference-value helpers plus <see cref="NoConfigValueException"/>.
/// These are pure utilities with public surface, so they are exercised directly
/// against a minimal in-memory <see cref="IConfiguration"/> (only the indexer
/// is used by the implementation) instead of through the CLI entry point.
/// </summary>
public sealed class ConfigurationRefValueTests
{
    [Fact]
    public void TryGetRefValue_PlainValue_ReturnsValue()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> { ["ApiKey"] = "secret-123" });

        var found = configuration.TryGetRefValue("ApiKey", out var value);

        Assert.True(found);
        Assert.Equal("secret-123", value);
    }

    [Fact]
    public void TryGetRefValue_MissingKey_ReturnsFalseWithNull()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>());

        var found = configuration.TryGetRefValue("Missing", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetRefValue_EmptyValue_ReturnsFalseWithNull()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> { ["Empty"] = string.Empty });

        var found = configuration.TryGetRefValue("Empty", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetRefValue_SingleReference_ResolvesTarget()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Actual"] = "resolved-value",
            ["Alias"] = "@ref:Actual",
        });

        var found = configuration.TryGetRefValue("Alias", out var value);

        Assert.True(found);
        Assert.Equal("resolved-value", value);
    }

    [Fact]
    public void TryGetRefValue_ChainedReferences_ResolvesFinalValue()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Final"] = "deep-value",
            ["Middle"] = "@ref:Final",
            ["First"] = "@ref:Middle",
        });

        var found = configuration.TryGetRefValue("First", out var value);

        Assert.True(found);
        Assert.Equal("deep-value", value);
    }

    [Fact]
    public void TryGetRefValue_DanglingReference_ReturnsFalseWithNull()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> { ["Alias"] = "@ref:Nowhere" });

        var found = configuration.TryGetRefValue("Alias", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void ContainsRefValue_ReflectsResolvability()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Present"] = "yes",
            ["Target"] = "yes",
            ["Alias"] = "@ref:Target",
        });

        Assert.True(configuration.ContainsRefValue("Present"));
        Assert.True(configuration.ContainsRefValue("Alias"));
        Assert.False(configuration.ContainsRefValue("Missing"));
    }

    [Fact]
    public void GetRefValue_ResolvesChainedReference()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Final"] = "deep-value",
            ["Alias"] = "@ref:Final",
        });

        Assert.Equal("deep-value", configuration.GetRefValue("Alias"));
    }

    [Fact]
    public void GetRefValue_MissingKey_ThrowsWithKeyName()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>());

        var exception = Assert.Throws<NoConfigValueException>(() => configuration.GetRefValue("DbPassword"));

        Assert.Contains("DbPassword", exception.Message);
        Assert.Contains("config is empty", exception.Message);
    }

    [Fact]
    public void GetRefValueOrDefault_PrefersValueAndFallsBack()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> { ["Present"] = "kept" });

        Assert.Equal("kept", configuration.GetRefValueOrDefault("Present", "fallback"));
        Assert.Equal("fallback", configuration.GetRefValueOrDefault("Missing", "fallback"));
        Assert.Null(configuration.GetRefValueOrDefault("Missing"));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new InMemoryConfiguration(values);

    private sealed class InMemoryConfiguration(Dictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get => values.TryGetValue(key, out var value) ? value : null;
            set => values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public IChangeToken GetReloadToken() => NeverChangeToken.Instance;
        public IConfigurationSection GetSection(string key) => new InMemorySection(key, this[key]);
        public IEnumerator<IConfigurationSection> GetEnumerator() =>
            values.Select(pair => (IConfigurationSection)new InMemorySection(pair.Key, pair.Value)).GetEnumerator();
    }

    private sealed class InMemorySection(string key, string? value) : IConfigurationSection
    {
        public string Key { get; } = key;
        public string Path { get; } = key;
        public string? Value { get; set; } = value;
        public string? this[string key] { get => null; set { } }
        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public IChangeToken GetReloadToken() => NeverChangeToken.Instance;
        public IConfigurationSection GetSection(string key) => new InMemorySection(key, null);
    }

    private sealed class NeverChangeToken : IChangeToken
    {
        public static readonly NeverChangeToken Instance = new();
        public bool ActiveChangeCallbacks => false;
        public bool HasChanged => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => EmptyDisposable.Instance;

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
