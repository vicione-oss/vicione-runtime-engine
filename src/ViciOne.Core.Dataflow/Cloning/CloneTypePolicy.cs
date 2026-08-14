using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Core.Dataflow.Cloning;

/// <summary>
/// Describes how values of a concrete runtime type participate in a clone operation.
/// </summary>
internal enum CloneBehavior
{
    DeepClone,
    Reference,
    Ignore,
}

/// <summary>
/// Central, ALC-safe type policy used by every clone path.
/// </summary>
internal static class CloneTypePolicy
{
    private sealed class CacheEntry(CloneBehavior behavior)
    {
        internal CloneBehavior Behavior { get; } = behavior;
    }

    private static readonly ConditionalWeakTable<Type, CacheEntry> s_cache = [];

    private static readonly string[] s_proxyTypeNamePrefixes =
    [
        "Castle.Proxies.",
        "System.Data.Entity.DynamicProxies.",
        "NHibernate.Proxy.",
    ];

    internal static CloneBehavior GetBehavior(Type type)
        => s_cache.GetValue(type, static t => new CacheEntry(Evaluate(t))).Behavior;

    private static CloneBehavior Evaluate(Type type)
    {
        if (ImmutableTypes.IsShallowCopyable(type))
            return CloneBehavior.Reference;

        if (typeof(Delegate).IsAssignableFrom(type))
            return CloneBehavior.Reference;

        if (typeof(MemberInfo).IsAssignableFrom(type) || typeof(Assembly).IsAssignableFrom(type))
            return CloneBehavior.Reference;

        if (typeof(System.Runtime.ConstrainedExecution.CriticalFinalizerObject).IsAssignableFrom(type))
            return CloneBehavior.Reference;

        if (IsKnownImmutableReference(type) || IsRuntimeManagedReference(type))
            return CloneBehavior.Reference;

        if (IsRuntimeProxy(type))
            return CloneBehavior.Reference;

        return CloneBehavior.DeepClone;
    }

    private static bool IsKnownImmutableReference(Type type)
    {
        if (type == typeof(DBNull) || type == typeof(Uri) || type == typeof(Version))
            return true;

        if (typeof(TimeZoneInfo).IsAssignableFrom(type))
            return true;

        return IsFrameworkComparer(type);
    }

    private static bool IsRuntimeManagedReference(Type type)
    {
        if (type.IsCOMObject)
            return true;

        if (typeof(Task).IsAssignableFrom(type) || typeof(Thread).IsAssignableFrom(type))
            return true;

        if (typeof(WaitHandle).IsAssignableFrom(type) ||
            typeof(CancellationTokenSource).IsAssignableFrom(type) ||
            typeof(AssemblyLoadContext).IsAssignableFrom(type))
        {
            return true;
        }

        if (type == typeof(WeakReference) || IsConstructedGeneric(type, typeof(WeakReference<>)) ||
            IsConstructedGeneric(type, typeof(Lazy<>)))
        {
            return true;
        }

        // I/O objects wrap a stream or handle they do not own exclusively. A field copy cannot
        // establish independent ownership, so a second wrapper would compete for the same
        // resource and could close it underneath the original.
        if (typeof(Stream).IsAssignableFrom(type) ||
            typeof(TextReader).IsAssignableFrom(type) ||
            typeof(TextWriter).IsAssignableFrom(type))
        {
            return true;
        }

        // Implementing IDisposable is deliberately not a reason to share. A data object that
        // merely happens to be disposable owns nothing the runtime releases on its own, and the
        // caller asked for an independent copy. Only automatic cleanup makes duplication unsafe:
        // duplicating a finalizable object can execute cleanup twice even when it does not
        // derive from CriticalFinalizerObject.
        MethodInfo? finalizer = type.GetMethod(
            "Finalize",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        return finalizer is not null && finalizer.DeclaringType != typeof(object);
    }

    /// <summary>
    /// Framework comparers — <see cref="EqualityComparer{T}.Default"/>, <see cref="Comparer{T}.Default"/>,
    /// <see cref="ReferenceEqualityComparer"/>, the <see cref="StringComparer"/> implementations — are
    /// stateless or immutable, and callers compare them by reference. Implementing a comparer interface
    /// is deliberately not enough on its own: a data object that also carries a comparison rule is data,
    /// and sharing it would hand the caller a clone that aliases the original.
    /// </summary>
    private static bool IsFrameworkComparer(Type type)
        => type.Assembly == typeof(object).Assembly && IsComparer(type);

    private static bool IsComparer(Type type)
    {
        if (typeof(IComparer).IsAssignableFrom(type) || typeof(IEqualityComparer).IsAssignableFrom(type))
            return true;

        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
                continue;

            Type definition = interfaceType.GetGenericTypeDefinition();
            if (definition == typeof(IComparer<>) || definition == typeof(IEqualityComparer<>))
                return true;
        }

        return false;
    }

    private static bool IsConstructedGeneric(Type type, Type genericDefinition)
        => type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition;

    private static bool IsRuntimeProxy(Type type)
    {
        if (type.Assembly.IsDynamic)
            return true;

        string? fullName = type.FullName;
        if (fullName is null)
            return false;

        foreach (string prefix in s_proxyTypeNamePrefixes)
        {
            if (fullName.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
