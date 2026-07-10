using System;
using System.Runtime.CompilerServices;

namespace ViciOne.Core.Dataflow.Cloning;

/// <summary>
/// ALC-safe, reflection-based deep cloner.
/// Delegates are generated once per concrete runtime type via compiled expression trees
/// and cached in a <see cref="ConditionalWeakTable{TKey,TValue}"/> so that entries are
/// automatically collected when the owning <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// unloads — no explicit <c>ClearCache()</c> required.
/// </summary>
public static class DeepCloner
{
    // ConditionalWeakTable provides ephemeron semantics: when a Type key becomes
    // unreachable from outside this table the entry is eligible for GC collection,
    // even if the stored CloneDelegate internally references that same Type.
    // GetValue(key, factory) is concurrency-safe; the factory may be invoked more
    // than once under contention but producing the same delegate twice is harmless.
    private static readonly ConditionalWeakTable<Type, CloneDelegate> s_cache = [];

    /// <summary>
    /// Produces a deep clone of <paramref name="obj"/>.
    /// <list type="bullet">
    ///   <item><description>Returns <see langword="null"/> when <paramref name="obj"/> is <see langword="null"/>.</description></item>
    ///   <item><description>Returns the value as-is for primitives, <see langword="string"/>, and other known-immutable types.</description></item>
    ///   <item><description>Handles circular and shared references: each visited instance appears exactly once in the clone graph.</description></item>
    ///   <item><description>Works on any type — no <see cref="ICloneable"/>, attributes, or parameterless constructor required.</description></item>
    /// </list>
    /// </summary>
    public static T? DeepClone<T>(T? obj)
    {
        if (obj is null)
            return default;

        // Use the actual runtime type so that subclasses are handled correctly.
        Type type = obj.GetType();

        if (ImmutableTypes.IsShallowCopyable(type))
            return obj;

        CloneDelegate del = GetOrCreateDelegate(type);
        var context = new CloneContext();

        // Boxing value types here is intentional: the delegate operates on object.
        return (T?)del(obj, context);
    }

    /// <summary>
    /// Returns the cached clone delegate for <paramref name="type"/>, building and
    /// caching it on first access. Internal so that <see cref="CloneDelegateFactory"/>
    /// can call back into this method for recursive field cloning.
    /// </summary>
    internal static CloneDelegate GetOrCreateDelegate(Type type)
        => s_cache.GetValue(type, CloneDelegateFactory.Build);
}
