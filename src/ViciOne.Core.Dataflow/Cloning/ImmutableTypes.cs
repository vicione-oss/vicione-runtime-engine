using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ViciOne.Core.Dataflow.Cloning;

/// <summary>
/// Determines whether a type's value can be safely shallow-copied (i.e. does not require
/// deep cloning because its contents are immutable or contain no reachable reference state
/// that the caller could observe changing).
/// </summary>
internal static class ImmutableTypes
{
    // Memoized per Type: the check runs on every object visited during a clone, and
    // for structs it recursively reflects over all fields. ConditionalWeakTable keeps
    // the cache ALC-safe (entries die with their Type), matching DeepCloner's delegate
    // cache; the boxed results are shared so no allocation occurs after first lookup.
    private static readonly ConditionalWeakTable<Type, object> s_cache = [];
    private static readonly object s_true = true;
    private static readonly object s_false = false;

    /// <summary>
    /// Returns <see langword="true"/> when a value of <paramref name="type"/> can be copied
    /// without further recursion: primitives, enums, known immutable BCL structs/classes,
    /// pointer types, and pure value types (structs whose every field is itself
    /// shallow-copyable).
    /// </summary>
    internal static bool IsShallowCopyable(Type type)
        => (bool)s_cache.GetValue(type, static t => Evaluate(t) ? s_true : s_false);

    private static bool Evaluate(Type type)
    {
        if (type.IsPrimitive) return true;
        if (type.IsEnum) return true;
        if (type.IsPointer) return true;
        if (type == typeof(string)) return true;
        if (type == typeof(decimal)) return true;
        if (type == typeof(DateTime)) return true;
        if (type == typeof(DateTimeOffset)) return true;
        if (type == typeof(TimeSpan)) return true;
        if (type == typeof(Guid)) return true;
        if (type == typeof(IntPtr)) return true;
        if (type == typeof(UIntPtr)) return true;

        // Runtime reflection objects (Type, MemberInfo and subtypes, Assembly) are
        // identity-safe reference singletons managed by the CLR; deep-cloning them
        // would corrupt the type system.
        if (typeof(MemberInfo).IsAssignableFrom(type)) return true;
        if (typeof(Assembly).IsAssignableFrom(type)) return true;

        // Critical finalizers (SafeHandle, CriticalHandle, Thread) guard native
        // resources: duplicating one copies the raw handle, and the clone's
        // finalizer would release the original's native resource (double free).
        if (typeof(System.Runtime.ConstrainedExecution.CriticalFinalizerObject).IsAssignableFrom(type)) return true;

        // Structs whose every instance field is also shallow-copyable need no deep clone.
        if (type.IsValueType && IsPureValueType(type))
            return true;

        return false;
    }

    private static bool IsPureValueType(Type type)
    {
        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!IsShallowCopyable(field.FieldType))
                return false;
        }

        return true;
    }
}
