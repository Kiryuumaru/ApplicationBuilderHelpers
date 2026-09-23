using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Factory delegate for building a per-<see cref="Type"/> plan.
/// Carries the trim annotation on the parameter.
/// </summary>
internal delegate TValue PlanFactory<out TValue>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType);

/// <summary>
/// Thread-safe per-<see cref="Type"/> plan cache.
/// The annotated <see cref="Type"/> key flows into the factory with the
/// annotated <see cref="PlanFactory{TValue}"/> delegate; each kind holds its own
/// instance (reflection descriptors per builder, injection plans global).
/// </summary>
internal sealed class TypePlanCache<TValue>
{
    private readonly ConcurrentDictionary<Type, TValue> _cache = new();
    private readonly object _syncRoot = new();
    private int _buildCount;

    /// <summary>
    /// Number of times the factory ran (cache misses).
    /// </summary>
    internal int BuildCount => _buildCount;

    /// <summary>
    /// Gets the cached plan for the type, building it once on first use.
    /// </summary>
    internal TValue GetOrAdd(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType,
        PlanFactory<TValue> factory)
    {
        if (_cache.TryGetValue(commandType, out var cached))
        {
            return cached;
        }

        lock (_syncRoot)
        {
            if (_cache.TryGetValue(commandType, out cached))
            {
                return cached;
            }

            var built = factory(commandType);
            _cache[commandType] = built;
            Interlocked.Increment(ref _buildCount);
            return built;
        }
    }
}
