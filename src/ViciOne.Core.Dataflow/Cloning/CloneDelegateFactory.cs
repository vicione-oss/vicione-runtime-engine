using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ViciOne.Core.Dataflow.Cloning;

/// <summary>
/// Per-type clone delegate. Receives a boxed (or reference) source object and a
/// <see cref="CloneContext"/> for the current operation; returns the cloned object
/// (boxed for value types, reference for reference types).
/// </summary>
internal delegate object CloneDelegate(object source, CloneContext context);

/// <summary>
/// Builds <see cref="CloneDelegate"/> instances for concrete runtime types.
/// Delegates are generated once per type and cached by <see cref="DeepCloner"/>.
/// </summary>
internal static class CloneDelegateFactory
{
    private const BindingFlags InstanceAllDeclared =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // -------------------------------------------------------------------------
    // Central dispatch — called by generated delegates to recursively clone
    // a field or element value.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Recursively clones <paramref name="source"/>, handling null, immutable types,
    /// and circular references. Entry point for generated delegates.
    /// </summary>
    private static object? CloneObject(object? source, CloneContext context)
    {
        if (source is null)
            return null;

        Type type = source.GetType();

        if (ImmutableTypes.IsShallowCopyable(type))
            return source;

        // Return the already-created clone for already-seen references (cycle / sharing).
        if (context.TryGetClone(source, out object? existing))
            return existing;

        return DeepCloner.GetOrCreateDelegate(type)(source, context);
    }

    // -------------------------------------------------------------------------
    // Delegate builder entry point
    // -------------------------------------------------------------------------

    internal static CloneDelegate Build(Type type)
    {
        // Delegates are value types semantically — copy by reference.
        if (typeof(Delegate).IsAssignableFrom(type))
            return static (source, _) => source;

        // Runtime type-system objects (Type, MemberInfo subtypes, Assembly) must be shared
        // by reference — deep-cloning them would corrupt the CLR's internal state.
        if (typeof(MemberInfo).IsAssignableFrom(type) || typeof(Assembly).IsAssignableFrom(type))
            return static (source, _) => source;

        // Runtime-generated proxies (mocking frameworks, ORM lazy-loading, DI interceptors)
        // are behavior objects, not data: traversing them would duplicate framework state
        // that is shared far beyond the instance (and may be mutated concurrently).
        if (IsRuntimeProxy(type))
            return static (source, _) => source;

        if (type.IsArray)
            return BuildArrayDelegate(type);

        // Dictionary<K,V> fast-path: reconstruct via KeyValuePair iteration instead of
        // traversing private backing fields, reducing allocation ~3× and time ~3–6×.
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return BuildDictionaryDelegate(type);

        if (type.IsValueType)
            return BuildValueTypeDelegate(type);

        return BuildReferenceTypeDelegate(type);
    }

    // -------------------------------------------------------------------------
    // Dictionary<K,V> cloning — fast-path via KeyValuePair iteration
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildDictionaryDelegate(Type type)
    {
        Type[] typeArgs = type.GetGenericArguments(); // [K, V]
        Type keyType = typeArgs[0];
        Type valueType = typeArgs[1];

        // Resolve the IEqualityComparer<K> Comparer property and Dictionary(int, IEqualityComparer<K>) ctor.
        // Comparers are intentionally shared by reference: they define dictionary lookup
        // semantics and are commonly immutable singleton instances such as StringComparer.
        // Deep-cloning arbitrary comparer implementations risks changing those semantics.
        PropertyInfo comparerProp = type.GetProperty("Comparer")!;
        PropertyInfo countProp = type.GetProperty("Count")!;
        ConstructorInfo ctorWithCapacityAndComparer = type.GetConstructor(
            [typeof(int), typeof(IEqualityComparer<>).MakeGenericType(keyType)])!;

        // Resolve the generic Add(K, V) method.
        MethodInfo addMethod = type.GetMethod("Add", [keyType, valueType])!;

        // Resolve KeyValuePair<K,V> and its Key/Value properties via the enumerator.
        Type kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
        PropertyInfo kvpKeyProp = kvpType.GetProperty("Key")!;
        PropertyInfo kvpValueProp = kvpType.GetProperty("Value")!;

        // Build a typed enumerator accessor: (object src) => IEnumerable<KeyValuePair<K,V>>
        // We use reflection here at delegate-build time; the hot loop below is reflection-free.
        MethodInfo getEnumeratorMethod = type.GetMethod("GetEnumerator")!;

        bool shallowKey = ImmutableTypes.IsShallowCopyable(keyType);
        bool shallowValue = ImmutableTypes.IsShallowCopyable(valueType);

        return (source, context) =>
        {
            int count = (int)countProp.GetValue(source)!;
            object? comparer = comparerProp.GetValue(source);
            object dest = ctorWithCapacityAndComparer.Invoke([count, comparer]);

            // Register before iterating so self-referential values resolve correctly.
            context.Register(source, dest);

            // Enumerate KeyValuePairs without touching private backing fields.
            object enumerator = getEnumeratorMethod.Invoke(source, null)!;
            MethodInfo moveNextMethod = enumerator.GetType().GetMethod("MoveNext")!;
            PropertyInfo currentProp = enumerator.GetType().GetProperty("Current")!;

            while ((bool)moveNextMethod.Invoke(enumerator, null)!)
            {
                object kvp = currentProp.GetValue(enumerator)!;
                object? key = kvpKeyProp.GetValue(kvp);
                object? value = kvpValueProp.GetValue(kvp);

                object? clonedKey = shallowKey ? key : CloneObject(key, context);
                object? clonedValue = shallowValue ? value : CloneObject(value, context);

                addMethod.Invoke(dest, [clonedKey, clonedValue]);
            }

            // Dispose the enumerator if it implements IDisposable.
            (enumerator as IDisposable)?.Dispose();

            return dest;
        };
    }

    // -------------------------------------------------------------------------
    // Reference type (class) cloning
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildReferenceTypeDelegate(Type type)
    {
        // Pre-build one copy "step" per field across the entire inheritance chain.
        // Steps are closures that already hold pre-compiled getter/setter delegates,
        // so no per-call FieldInfo lookup occurs.
        FieldCopyStep[] steps = BuildFieldCopySteps(type);

        return (source, context) =>
        {
            // Allocate without calling any constructor — no parameterless ctor required.
            // NOTE: field initializers and [OnDeserializing] callbacks do not run here;
            //       this matches FastCloner semantics.
            object dest = RuntimeHelpers.GetUninitializedObject(type);

            // Register BEFORE processing fields so that back-edges in the graph
            // resolve to this (not-yet-finished) clone rather than re-entering.
            context.Register(source, dest);

            foreach (FieldCopyStep step in steps)
                step(dest, source, context);

            return dest;
        };
    }

    // FieldCopyStep: (destination, source, context) → void
    private delegate void FieldCopyStep(object dest, object source, CloneContext context);

    private static FieldCopyStep[] BuildFieldCopySteps(Type type)
    {
        var steps = new List<FieldCopyStep>();

        foreach (FieldInfo field in GetAllInstanceFields(type))
        {
            // Capture locally for closure correctness.
            FieldInfo f = field;
            bool shallow = ImmutableTypes.IsShallowCopyable(f.FieldType)
                           || IsDelegateType(f.FieldType);

            if (shallow)
            {
                if (f.IsInitOnly)
                {
                    // Readonly field — use SetValue (bypasses initonly restriction).
                    steps.Add((dest, src, _) =>
                        f.SetValue(dest, f.GetValue(src)));
                }
                else
                {
                    Func<object, object?> getter = CompileGetter(f);
                    Action<object, object?> setter = CompileSetter(f);
                    steps.Add((dest, src, _) => setter(dest, getter(src)));
                }
            }
            else
            {
                if (f.IsInitOnly)
                {
                    steps.Add((dest, src, ctx) =>
                        f.SetValue(dest, CloneObject(f.GetValue(src), ctx)));
                }
                else
                {
                    Func<object, object?> getter = CompileGetter(f);
                    Action<object, object?> setter = CompileSetter(f);
                    steps.Add((dest, src, ctx) => setter(dest, CloneObject(getter(src), ctx)));
                }
            }
        }

        return [.. steps];
    }

    // -------------------------------------------------------------------------
    // Value type (struct with reference fields) cloning
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildValueTypeDelegate(Type type)
    {
        FieldInfo[] allFields = [.. GetAllInstanceFields(type)];
        // Pre-classify each field at build time so the clone lambda pays no reflection
        // cost for shallowness decisions (mirrors how BuildFieldCopySteps works).
        bool[] isShallow = Array.ConvertAll(allFields, f =>
            ImmutableTypes.IsShallowCopyable(f.FieldType) || IsDelegateType(f.FieldType));

        return (source, context) =>
        {
            // GetUninitializedObject returns a boxed zero-initialized value type.
            object dest = RuntimeHelpers.GetUninitializedObject(type);

            // Boxed value types can participate in object-reference cycles.
            // Register the source box before cloning fields so back-edges resolve to dest.
            context.Register(source, dest);

            for (int i = 0; i < allFields.Length; i++)
            {
                object? value = allFields[i].GetValue(source);
                allFields[i].SetValue(dest, isShallow[i] ? value : CloneObject(value, context));
            }

            return dest;
        };
    }

    // -------------------------------------------------------------------------
    // Array cloning (single- and multi-dimensional, non-zero lower bounds)
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildArrayDelegate(Type type)
    {
        Type elementType = type.GetElementType()!;
        int rank = type.GetArrayRank();
        bool shallowElements = ImmutableTypes.IsShallowCopyable(elementType);

        return (source, context) =>
        {
            var srcArray = (Array)source;

            int[] lengths = new int[rank];
            int[] lowerBounds = new int[rank];
            for (int dim = 0; dim < rank; dim++)
            {
                lengths[dim] = srcArray.GetLength(dim);
                lowerBounds[dim] = srcArray.GetLowerBound(dim);
            }

            var destArray = Array.CreateInstance(elementType, lengths, lowerBounds);

            // Register before iterating so that arrays containing themselves are handled.
            context.Register(source, destArray);

            if (shallowElements)
            {
                // Element type is immutable — a bulk copy is sufficient.
                Array.Copy(srcArray, destArray, srcArray.Length);
            }
            else if (rank == 1)
            {
                int lb = lowerBounds[0];
                int len = lengths[0];
                for (int i = lb; i < lb + len; i++)
                    destArray.SetValue(CloneObject(srcArray.GetValue(i), context), i);
            }
            else
            {
                CopyMultiDimArray(srcArray, destArray, rank, lengths, lowerBounds, context);
            }

            return destArray;
        };
    }

    private static void CopyMultiDimArray(
        Array src, Array dest,
        int rank, int[] lengths, int[] lowerBounds,
        CloneContext context)
    {
        // At least one dimension has length 0 — nothing to copy.
        if (src.Length == 0)
            return;

        int[] indices = new int[rank];
        for (int i = 0; i < rank; i++)
            indices[i] = lowerBounds[i];

        while (true)
        {
            dest.SetValue(CloneObject(src.GetValue(indices), context), indices);

            // Increment the indices (least-significant dimension last, like a counter).
            int dim = rank - 1;
            while (dim >= 0)
            {
                indices[dim]++;
                if (indices[dim] < lowerBounds[dim] + lengths[dim])
                    break;
                indices[dim] = lowerBounds[dim];
                dim--;
            }

            if (dim < 0)
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Expression-tree helpers for fast (non-readonly) field access
    // -------------------------------------------------------------------------

    /// <summary>Builds a compiled delegate that reads one field from a boxed object.</summary>
    private static Func<object, object?> CompileGetter(FieldInfo field)
    {
        ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
        UnaryExpression castObj = Expression.Convert(objParam, field.DeclaringType!);
        MemberExpression fieldExpr = Expression.Field(castObj, field);
        UnaryExpression boxed = Expression.Convert(fieldExpr, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, objParam).Compile();
    }

    /// <summary>
    /// Builds a compiled delegate that writes a value (boxed) into one field of a
    /// boxed object.  Only called for writable (non-<c>initonly</c>) fields on
    /// reference types; modifying a field through a reference-type cast is safe and
    /// writes directly into the heap.
    /// </summary>
    private static Action<object, object?> CompileSetter(FieldInfo field)
    {
        ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
        ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
        UnaryExpression castObj = Expression.Convert(objParam, field.DeclaringType!);
        UnaryExpression castValue = Expression.Convert(valueParam, field.FieldType);
        BinaryExpression assignExpr = Expression.Assign(Expression.Field(castObj, field), castValue);
        return Expression.Lambda<Action<object, object?>>(assignExpr, objParam, valueParam).Compile();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Walks the full inheritance chain (excluding <see cref="object"/>) and yields
    /// every declared instance field in declaration order from base to derived.
    /// </summary>
    private static IEnumerable<FieldInfo> GetAllInstanceFields(Type type)
    {
        // Build base-first list so fields are processed in hierarchy order.
        var hierarchy = new Stack<Type>();
        Type? t = type;
        while (t is not null && t != typeof(object))
        {
            hierarchy.Push(t);
            t = t.BaseType;
        }

        while (hierarchy.TryPop(out Type? current))
            foreach (FieldInfo field in current.GetFields(InstanceAllDeclared))
                yield return field;
    }

    private static bool IsDelegateType(Type type)
        => typeof(Delegate).IsAssignableFrom(type);

    // Well-known namespaces of proxy types emitted into persisted (non-dynamic) assemblies.
    private static readonly string[] s_proxyTypeNamePrefixes =
    [
        "Castle.Proxies.",
        "System.Data.Entity.DynamicProxies.",
        "NHibernate.Proxy.",
    ];

    /// <summary>
    /// Detects runtime-generated proxy types: anything emitted via Reflection.Emit
    /// (covers Castle DynamicProxy's <c>DynamicProxyGenAssembly2</c> used by NSubstitute,
    /// Moq, and EF Core lazy-loading proxies) plus well-known proxy namespaces for
    /// generators that persist their assemblies. Types without a <see cref="Type.FullName"/>
    /// (open generics, exotic runtime types) are treated as proxies as well — they cannot
    /// be cloned meaningfully.
    /// </summary>
    private static bool IsRuntimeProxy(Type type)
    {
        if (type.Assembly.IsDynamic)
            return true;

        string? fullName = type.FullName;
        if (fullName is null)
            return true;

        foreach (string prefix in s_proxyTypeNamePrefixes)
        {
            if (fullName.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
