using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Factory delegate for building a per-<see cref="Type"/> plan.
/// Carries the trim annotation on the parameter so the
/// <c>DynamicallyAccessedMembers</c> dataflow survives the delegate hop.
/// Generic Func with a Type parameter cannot carry that annotation.
/// </summary>
internal delegate TValue PlanFactory<out TValue>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType);

/// <summary>
/// Shared double-checked-lock (DCL) per-<see cref="Type"/> plan cache.
/// Trim-safe by construction: the annotated <see cref="Type"/> key flows
/// into the factory via the annotated <see cref="PlanFactory{TValue}"/>
/// delegate hop — callers pass their own
/// <c>DynamicallyAccessedMembers</c>-annotated method group
/// (<c>GetOrAdd(commandType, Build)</c>) with no lambda capture shedding
/// the annotation (method-group delegate creation reports IL2111, suppressed
/// explicitly at the call sites). Each kind owns an instance (reflection
/// descriptors per builder, injection plans global), so the per-kind
/// build counter stays per-kind. DCL (not Lazy):
/// the miss counter increments exactly when a new entry is published.
/// </summary>
internal sealed class TypePlanCache<TValue>
{
    private readonly ConcurrentDictionary<Type, TValue> _cache = new();
    private readonly object _syncRoot = new();
    private int _buildCount;

    /// <summary>
    /// Number of times the factory ran (cache misses). Test hook.
    /// </summary>
    internal int BuildCount => _buildCount;

    /// <summary>
    /// Gets the cached plan for the type, building it once on first use.
    /// The miss counter increments exactly when a new entry is published.
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
