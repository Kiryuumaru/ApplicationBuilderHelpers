using Application.Shared.Extensions;
using Application.Logger.Extensions;
using Infrastructure.OpenTelemetry.Interfaces;
using Infrastructure.OpenTelemetry.LogEventPropertyTypes;
using Microsoft.Extensions.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Infrastructure.OpenTelemetry.Enrichers;

internal class DefaultLogEnricher(IConfiguration configuration, Dictionary<string, object?>? scopeMap) : ILogEventEnricher
{
    private const string ValueTypeMapIdentifier = "ValueTypeMap";
    private const int MaxSourceContextsToKeep = 10; // Limit the number of source contexts to prevent unbounded growth
    
    private readonly Dictionary<string, ILogEventPropertyParser> _propertyParserMap = new()
    {
        [BooleanPropertyParser.Default.TypeIdentifier] = BooleanPropertyParser.Default,
        [DateTimeOffsetPropertyParser.Default.TypeIdentifier] = DateTimeOffsetPropertyParser.Default,
        [DateTimePropertyParser.Default.TypeIdentifier] = DateTimePropertyParser.Default,
        [GuidPropertyParser.Default.TypeIdentifier] = GuidPropertyParser.Default,
        [IntPropertyParser.Default.TypeIdentifier] = IntPropertyParser.Default,
        [LongPropertyParser.Default.TypeIdentifier] = LongPropertyParser.Default,
        [ShortPropertyParser.Default.TypeIdentifier] = ShortPropertyParser.Default,
        [StringPropertyParser.Default.TypeIdentifier] = StringPropertyParser.Default,
        [UriPropertyParser.Default.TypeIdentifier] = UriPropertyParser.Default,
    };

    private static bool _hasHeadRuntimeLogs = false;
    private static Guid? _runtimeGuid = null;
    private static readonly Lock _runtimeGuidLock = new();

    public void Enrich(LogEvent evt, ILogEventPropertyFactory _)
    {
        if (_runtimeGuid == null)
        {
            lock (_runtimeGuidLock)
            {
                _runtimeGuid ??= configuration.GetRuntimeGuid();
            }
        }

        AddProperty(evt, "EventGuid", Guid.NewGuid(), false);
        AddProperty(evt, "RuntimeGuid", _runtimeGuid.Value, false);
        
        if (!_hasHeadRuntimeLogs)
        {
            lock (_runtimeGuidLock)
            {
                if (!_hasHeadRuntimeLogs)
                {
                    AddProperty(evt, "IsHeadLog", true, false);
                    _hasHeadRuntimeLogs = true;
                }
            }
        }

        if (scopeMap != null && scopeMap.Count > 0)
        {
            foreach (var prop in scopeMap)
            {
                AddProperty(evt, prop.Key, prop.Value, addAndReplace: true);
            }
        }

        EnrichLogSourceContext(evt);
        EnrichLogTypings(evt);
    }

    private static void EnrichLogSourceContext(LogEvent evt)
    {
        List<StructureValue> sourceContexts = [];
        List<string> keysToRemove = [];
        string? currentSourceContext = null;
        string? lastSourceContext = null;
        string? lastSourceContextAction = null;

        foreach (var prop in evt.Properties)
        {
            if (prop.Key.StartsWith(ILoggerExtensions.SourceContextActionIdentifierAndSeparator))
            {
                keysToRemove.Add(prop.Key);
                var split = prop.Key.Split(ILoggerExtensions.SourceContextActionSeparator);
                if (split.Length != 3)
                {
                    continue;
                }
                lastSourceContext = split[1];
                lastSourceContextAction = split[2];
                
                // Limit the number of source contexts to prevent unbounded memory growth
                if (sourceContexts.Count < MaxSourceContextsToKeep)
                {
                    sourceContexts.Add(new(
                    [
                        new LogEventProperty("Context", new ScalarValue(split[1])),
                        new LogEventProperty("Action", new ScalarValue(split[2])),
                    ]));
                }
            }
            else if (prop.Key.Equals("SourceContext"))
            {
                currentSourceContext = (prop.Value as ScalarValue)?.Value?.ToString();
            }
        }

        foreach (var key in keysToRemove)
        {
            RemoveProperty(evt, key);
        }

        if (sourceContexts.Count > 0)
        {
            evt.AddOrUpdateProperty(new LogEventProperty(ILoggerExtensions.SourceContextActionsIdentifier, new SequenceValue(sourceContexts)));
        }

        if (lastSourceContextAction != null && currentSourceContext == lastSourceContext)
        {
            AddProperty(evt, ILoggerExtensions.SourceContextActionIdentifier, lastSourceContextAction, addAndReplace: true);
        }
    }

    private void EnrichLogTypings(LogEvent evt)
    {
        StructureValue? realValueTypeIdentifierKey = evt.Properties.GetValueOrDefault(ValueTypeMapIdentifier) as StructureValue;
        Dictionary<string, LogEventProperty> logTypings = realValueTypeIdentifierKey?.Properties?.ToDictionary(i => i.Name, i => i) ?? [];
        List<LogEventProperty> propertiesToAdd = [];

        foreach (var prop in evt.Properties)
        {
            // Skip already processed typing properties to reduce overhead
            if (prop.Key == ValueTypeMapIdentifier || prop.Key.StartsWith("__"))
            {
                continue;
            }

            object? value = (prop.Value as ScalarValue)?.Value;
            Type? valueType = value == null ? null : GetUnderlyingType(value);

            if (valueType == null)
            {
                continue;
            }

            string? realValueLogTyping = null;
            if (logTypings.TryGetValue(prop.Key, out var existingValueLogTyping))
            {
                realValueLogTyping = (existingValueLogTyping.Value as ScalarValue)?.Value?.ToString()!;
            }

            realValueLogTyping ??= valueType.Name;

            if (!string.IsNullOrEmpty(realValueLogTyping) && _propertyParserMap.TryGetValue(realValueLogTyping, out var logEventPropertyParser))
            {
                LogEventProperty newValueTypingProp = new(prop.Key, new ScalarValue(logEventPropertyParser.Parse(value?.ToString())));
                propertiesToAdd.Add(newValueTypingProp);

                logTypings[prop.Key] = new(prop.Key, new ScalarValue(realValueLogTyping));
            }
        }

        foreach (var prop in propertiesToAdd)
        {
            evt.AddOrUpdateProperty(prop);
        }

        if (logTypings.Count > 0)
        {
            evt.AddOrUpdateProperty(new LogEventProperty(ValueTypeMapIdentifier, new StructureValue(logTypings.Values)));
        }
    }

    private static void AddProperty(LogEvent evt, string key, object? value, bool addAndReplace)
    {
        LogEventProperty logEventProperty = new(key, new ScalarValue(value));

        if (addAndReplace)
        {
            evt.AddOrUpdateProperty(logEventProperty);
        }
        else
        {
            evt.AddPropertyIfAbsent(logEventProperty);
        }
    }

    private static void RemoveProperty(LogEvent evt, string key)
    {
        evt.RemovePropertyIfPresent(key);
    }

    private static Type GetUnderlyingType(object obj)
    {
        Type valueType = obj.GetType();
        while (true)
        {
            var underlyingType = Nullable.GetUnderlyingType(valueType);
            if (underlyingType == null)
            {
                break;
            }
            valueType = underlyingType;
        }
        return valueType;
    }
}
