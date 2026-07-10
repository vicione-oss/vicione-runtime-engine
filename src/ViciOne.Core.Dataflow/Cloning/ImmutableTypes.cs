using System;
using System.Reflection;

namespace ViciOne.Core.Dataflow.Cloning;

/// <summary>
/// Determines whether a type's value can be safely shallow-copied (i.e. does not require
/// deep cloning because its contents are immutable or contain no reachable reference state
/// that the caller could observe changing).
/// </summary>
internal static class ImmutableTypes
{
    /// <summary>
    /// Returns <see langword="true"/> when a value of <paramref name="type"/> can be copied
    /// without further recursion: primitives, enums, known immutable BCL structs/classes,
    /// pointer types, and pure value types (structs whose every field is itself
    /// shallow-copyable).
    /// </summary>
    internal static bool IsShallowCopyable(Type type)
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
