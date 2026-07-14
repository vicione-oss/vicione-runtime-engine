using System;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        if (IsRuntimeProxy(type))
            return CloneBehavior.Reference;

        return CloneBehavior.DeepClone;
    }

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
