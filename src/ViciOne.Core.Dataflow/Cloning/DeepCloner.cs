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
    ///   <item><description>Clones supported data types without requiring <see cref="ICloneable"/>, attributes, or a parameterless constructor.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Intended for plain data graphs (settings, poolings, values), not arbitrary runtime
    /// or resource-owning objects. Objects that represent behavior or identity rather than
    /// data are shared by reference instead of cloned:
    /// delegates, <see cref="Type"/>/<see cref="System.Reflection.MemberInfo"/>/
    /// <see cref="System.Reflection.Assembly"/>, runtime-generated proxies (mocks, ORM
    /// lazy-loading proxies, DI interceptors), tasks, threads, wait handles, weak references,
    /// <see cref="Lazy{T}"/>, COM objects, streams and text readers/writers, finalizable types,
    /// framework comparers such as <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/>
    /// and the <see cref="StringComparer"/> implementations, and critical finalizers such as
    /// <see cref="System.Runtime.InteropServices.SafeHandle"/> guarding native resources.
    /// Implementing <see cref="IDisposable"/> or a comparer interface is by itself not one of these
    /// criteria: a data object that merely happens to be disposable, or to carry a comparison rule,
    /// is cloned like any other.
    /// </para>
    /// <para>
    /// Constructors, field initializers, property setters, and serialization callbacks are
    /// not invoked for ordinary objects. Mutable collection types use a constructor to create
    /// an empty shell before their contents are cloned; a constructor that seeds content is
    /// rejected. Immutable and frozen collections cannot be created empty, so they are rebuilt
    /// from their cloned elements with their comparers preserved; <c>ImmutableArray&lt;T&gt;</c>
    /// is cloned through its backing array. Because such a collection only exists once its
    /// contents do, it is rejected in three cases: when a reference cycle leads back into it while
    /// it is being built, when such builds nest deeper than the recursion guard described next
    /// allows, and — for the kinds that hash or order by element state, so not for
    /// <c>ImmutableList&lt;T&gt;</c>, <c>ImmutableQueue&lt;T&gt;</c> or
    /// <c>ImmutableStack&lt;T&gt;</c> — when the entries it hashes or orders by are not yet
    /// complete. Linked-list node ownership and read-only wrapper/backing-collection aliases are
    /// preserved as part of the graph.
    /// </para>
    /// <para>
    /// Deep graphs automatically switch to an explicit work stack to avoid exhausting the
    /// executing thread's call stack. A clone handed out under that guard is registered before
    /// it is populated, so collections that hash or order by element state fill themselves once
    /// the graph is complete rather than bucketing an empty shell.
    /// </para>
    /// <para>
    /// The source graph must not be mutated concurrently while it is cloned. Fields marked with
    /// <see cref="NonSerializedAttribute"/> are left at their default value.
    /// </para>
    /// </remarks>
    public static T? DeepClone<T>(T? obj)
    {
        if (obj is null)
            return default;

        // Use the actual runtime type so that subclasses are handled correctly.
        Type type = obj.GetType();

        if (CloneTypePolicy.GetBehavior(type) == CloneBehavior.Reference)
            return obj;

        CloneDelegate del = GetOrCreateDelegate(type);
        var context = new CloneContext();

        // Boxing value types here is intentional: the delegate operates on object.
        object clone = del(obj, context);
        context.DrainPendingWork();
        return (T?)clone;
    }

    /// <summary>
    /// Returns the cached clone delegate for <paramref name="type"/>, building and
    /// caching it on first access. Internal so that <see cref="CloneDelegateFactory"/>
    /// can call back into this method for recursive field cloning.
    /// </summary>
    internal static CloneDelegate GetOrCreateDelegate(Type type)
        => s_cache.GetValue(type, CloneDelegateFactory.Build);
}
