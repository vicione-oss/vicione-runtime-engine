using System;
using System.Collections;
using System.Collections.Concurrent;
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

/// <summary>Populates a previously allocated and registered clone shell.</summary>
internal delegate void ClonePopulateDelegate(object source, object destination, CloneContext context);

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

        switch (CloneTypePolicy.GetBehavior(type))
        {
            case CloneBehavior.Reference:
                return source;
            case CloneBehavior.Ignore:
                return null;
        }

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
        if (CloneTypePolicy.GetBehavior(type) != CloneBehavior.DeepClone)
            return static (source, _) => source;

        if (type.IsArray)
            return BuildArrayDelegate(type);

        Type? dictionaryBase = FindConstructedGenericBase(type, typeof(Dictionary<,>));
        if (dictionaryBase is not null)
            return BuildDictionaryDelegate(type, dictionaryBase);

        Type? concurrentDictionaryBase = FindConstructedGenericBase(type, typeof(ConcurrentDictionary<,>));
        if (concurrentDictionaryBase is not null)
            return BuildDictionaryDelegate(type, concurrentDictionaryBase);

        Type? hashSetBase = FindConstructedGenericBase(type, typeof(HashSet<>));
        if (hashSetBase is not null)
            return BuildHashSetDelegate(type, hashSetBase);

        Type? listBase = FindConstructedGenericBase(type, typeof(List<>));
        if (listBase is not null)
            return BuildSequenceDelegate(type, listBase, "Add", reverseSourceOrder: false);

        Type? queueBase = FindConstructedGenericBase(type, typeof(Queue<>));
        if (queueBase is not null)
            return BuildSequenceDelegate(type, queueBase, "Enqueue", reverseSourceOrder: false);

        Type? stackBase = FindConstructedGenericBase(type, typeof(Stack<>));
        if (stackBase is not null)
            return BuildSequenceDelegate(type, stackBase, "Push", reverseSourceOrder: true);

        Type? concurrentQueueBase = FindConstructedGenericBase(type, typeof(ConcurrentQueue<>));
        if (concurrentQueueBase is not null)
            return BuildSequenceDelegate(type, concurrentQueueBase, "Enqueue", reverseSourceOrder: false);

        Type? concurrentStackBase = FindConstructedGenericBase(type, typeof(ConcurrentStack<>));
        if (concurrentStackBase is not null)
            return BuildSequenceDelegate(type, concurrentStackBase, "Push", reverseSourceOrder: true);

        Type? concurrentBagBase = FindConstructedGenericBase(type, typeof(ConcurrentBag<>));
        if (concurrentBagBase is not null)
            return BuildSequenceDelegate(type, concurrentBagBase, "Add", reverseSourceOrder: false);

        if (type.IsValueType)
            return BuildValueTypeDelegate(type);

        return BuildReferenceTypeDelegate(type);
    }

    // -------------------------------------------------------------------------
    // Dictionary<K,V> cloning — fast-path via KeyValuePair iteration
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildDictionaryDelegate(Type type, Type dictionaryBase)
    {
        Type[] typeArgs = dictionaryBase.GetGenericArguments(); // [K, V]
        Type keyType = typeArgs[0];
        Type valueType = typeArgs[1];
        Type comparerType = typeof(IEqualityComparer<>).MakeGenericType(keyType);
        PropertyInfo comparerProp = dictionaryBase.GetProperty("Comparer")!;
        CollectionFactory factory = BuildCollectionFactory(type, comparerType);
        FieldCopyStep[] derivedSteps = type == dictionaryBase
            ? []
            : BuildFieldCopySteps(type, dictionaryBase);

        bool shallowKey = CloneTypePolicy.GetBehavior(keyType) == CloneBehavior.Reference;
        bool shallowValue = CloneTypePolicy.GetBehavior(valueType) == CloneBehavior.Reference;

        ClonePopulateDelegate populate = (source, destination, context) =>
        {
            var srcDict = (IDictionary)source;
            var dest = (IDictionary)destination;
            IDictionaryEnumerator enumerator = srcDict.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    object key = enumerator.Key;
                    object? value = enumerator.Value;
                    object? clonedKey = shallowKey ? key : CloneObject(key, context);
                    object? clonedValue = shallowValue ? value : CloneObject(value, context);

                    dest.Add(clonedKey!, clonedValue);
                }
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }

            foreach (FieldCopyStep step in derivedSteps)
                step(dest, source, context);
        };

        return (source, context) =>
        {
            var srcDict = (IDictionary)source;
            object? comparer = comparerProp.GetValue(source);
            var dest = (IDictionary)factory.Create(srcDict.Count, comparer);

            EnsureComparerPreserved(type, comparerProp, comparer, dest, factory.AcceptsComparer);
            context.Register(source, dest);
            context.PopulateOrDefer(source, dest, populate);

            return dest;
        };
    }

    // -------------------------------------------------------------------------
    // HashSet<T> cloning — rebuild so cloned elements are re-hashed
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildHashSetDelegate(Type type, Type hashSetBase)
    {
        Type elementType = hashSetBase.GetGenericArguments()[0];
        Type comparerType = typeof(IEqualityComparer<>).MakeGenericType(elementType);
        PropertyInfo comparerProp = hashSetBase.GetProperty("Comparer")!;
        PropertyInfo countProp = hashSetBase.GetProperty("Count")!;
        CollectionFactory factory = BuildCollectionFactory(type, comparerType);
        Action<object, object?> add = CompileCollectionAdd(hashSetBase, elementType);
        bool shallowElement = CloneTypePolicy.GetBehavior(elementType) == CloneBehavior.Reference;
        FieldCopyStep[] derivedSteps = type == hashSetBase
            ? []
            : BuildFieldCopySteps(type, hashSetBase);

        ClonePopulateDelegate populate = (source, destination, context) =>
        {
            foreach (object? item in (IEnumerable)source)
                add(destination, shallowElement ? item : CloneObject(item, context));

            foreach (FieldCopyStep step in derivedSteps)
                step(destination, source, context);
        };

        return (source, context) =>
        {
            int count = (int)countProp.GetValue(source)!;
            object? comparer = comparerProp.GetValue(source);
            object dest = factory.Create(count, comparer);

            EnsureComparerPreserved(type, comparerProp, comparer, dest, factory.AcceptsComparer);
            context.Register(source, dest);
            context.PopulateOrDefer(source, dest, populate);

            return dest;
        };
    }

    private sealed record CollectionFactory(
        Func<int, object?, object> Create,
        bool AcceptsComparer);

    private static CollectionFactory BuildCollectionFactory(Type type, Type comparerType)
    {
        const BindingFlags ConstructorFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        ConstructorInfo? capacityAndComparer = type.GetConstructor(
            ConstructorFlags, binder: null, [typeof(int), comparerType], modifiers: null);
        if (TakesOnlyCapacityInts(capacityAndComparer))
        {
            return new CollectionFactory(
                (capacity, comparer) => capacityAndComparer!.Invoke([capacity, comparer]),
                AcceptsComparer: true);
        }

        ConstructorInfo? comparerOnly = type.GetConstructor(
            ConstructorFlags, binder: null, [comparerType], modifiers: null);
        if (comparerOnly is not null)
        {
            return new CollectionFactory(
                (_, comparer) => comparerOnly.Invoke([comparer]),
                AcceptsComparer: true);
        }

        ConstructorInfo? capacityOnly = type.GetConstructor(
            ConstructorFlags, binder: null, [typeof(int)], modifiers: null);
        if (TakesOnlyCapacityInts(capacityOnly))
        {
            return new CollectionFactory(
                (capacity, _) => capacityOnly!.Invoke([capacity]),
                AcceptsComparer: false);
        }

        ConstructorInfo? parameterless = type.GetConstructor(
            ConstructorFlags, binder: null, Type.EmptyTypes, modifiers: null);
        if (parameterless is not null)
        {
            return new CollectionFactory(
                (_, _) => parameterless.Invoke(null),
                AcceptsComparer: false);
        }

        throw new NotSupportedException(
            $"Keyed collection type '{type}' has no constructor that can create a valid empty clone.");
    }

    /// <summary>
    /// <see langword="true"/> when every <see langword="int"/> the constructor declares is a capacity.
    /// The clone factory feeds the source's element count into those parameters, which is only correct
    /// when they mean capacity: a derived collection may declare an <see langword="int"/> for its own
    /// domain, and passing the element count would run foreign logic against a fabricated value.
    /// </summary>
    private static bool TakesOnlyCapacityInts(ConstructorInfo? constructor)
    {
        if (constructor is null)
            return false;

        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            if (parameter.ParameterType == typeof(int) &&
                !string.Equals(parameter.Name, "capacity", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureComparerPreserved(
        Type collectionType,
        PropertyInfo comparerProperty,
        object? sourceComparer,
        object destination,
        bool factoryAcceptsComparer)
    {
        if (factoryAcceptsComparer)
            return;

        object? destinationComparer = comparerProperty.GetValue(destination);
        if (ReferenceEquals(sourceComparer, destinationComparer))
            return;

        // Constructors are not inherited, so a derived collection never matches the comparer-taking
        // constructors and is built through its own parameterless one — which supplied a comparer of
        // its own. A stateless comparer has no configuration to lose, so any instance of that type
        // behaves identically and the shell is already correct.
        if (destinationComparer is not null &&
            sourceComparer is not null &&
            sourceComparer.GetType() == destinationComparer.GetType() &&
            IsStateless(sourceComparer.GetType()))
        {
            return;
        }

        throw new NotSupportedException(
            $"Keyed collection type '{collectionType}' cannot preserve its comparer when cloned.");
    }

    /// <summary>
    /// <see langword="true"/> when the type carries no instance state, so every instance of it is
    /// interchangeable with every other.
    /// </summary>
    private static bool IsStateless(Type type)
        => type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0;

    private static Action<object, object?> CompileCollectionAdd(Type collectionBase, Type elementType)
    {
        MethodInfo addMethod = collectionBase.GetMethod("Add", [elementType])!;
        ParameterExpression collection = Expression.Parameter(typeof(object), "collection");
        ParameterExpression item = Expression.Parameter(typeof(object), "item");
        MethodCallExpression call = Expression.Call(
            Expression.Convert(collection, collectionBase),
            addMethod,
            Expression.Convert(item, elementType));

        return Expression.Lambda<Action<object, object?>>(
            Expression.Block(call, Expression.Empty()),
            collection,
            item).Compile();
    }

    // -------------------------------------------------------------------------
    // Mutable sequential collections — rebuild through their public APIs so the
    // implementation's internal arrays, head indices, and version fields remain valid.
    // -------------------------------------------------------------------------

    private static CloneDelegate BuildSequenceDelegate(
        Type type,
        Type collectionBase,
        string insertionMethodName,
        bool reverseSourceOrder)
    {
        Type elementType = collectionBase.GetGenericArguments()[0];
        PropertyInfo countProperty = collectionBase.GetProperty("Count")!;
        Func<int, object> factory = BuildSequenceFactory(type);
        Action<object, object?> insert = CompileCollectionInsertion(
            collectionBase,
            elementType,
            insertionMethodName);
        bool shallowElement = CloneTypePolicy.GetBehavior(elementType) == CloneBehavior.Reference;
        FieldCopyStep[] derivedSteps = type == collectionBase
            ? []
            : BuildFieldCopySteps(type, collectionBase);

        ClonePopulateDelegate populate = (source, destination, context) =>
        {
            if (reverseSourceOrder)
            {
                // Stack<T> enumerates from top to bottom. Push the cloned snapshot in
                // reverse so the destination exposes the same top-to-bottom ordering.
                var items = new List<object?>();
                foreach (object? item in (IEnumerable)source)
                    items.Add(shallowElement ? item : CloneObject(item, context));

                for (int i = items.Count - 1; i >= 0; i--)
                    insert(destination, items[i]);
            }
            else
            {
                foreach (object? item in (IEnumerable)source)
                    insert(destination, shallowElement ? item : CloneObject(item, context));
            }

            foreach (FieldCopyStep step in derivedSteps)
                step(destination, source, context);
        };

        return (source, context) =>
        {
            int count = (int)countProperty.GetValue(source)!;
            object destination = factory(count);
            context.Register(source, destination);
            context.PopulateOrDefer(source, destination, populate);
            return destination;
        };
    }

    private static Func<int, object> BuildSequenceFactory(Type type)
    {
        const BindingFlags ConstructorFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        ConstructorInfo? capacityConstructor = type.GetConstructor(
            ConstructorFlags, binder: null, [typeof(int)], modifiers: null);
        if (TakesOnlyCapacityInts(capacityConstructor))
            return capacity => capacityConstructor!.Invoke([capacity]);

        ConstructorInfo? parameterlessConstructor = type.GetConstructor(
            ConstructorFlags, binder: null, Type.EmptyTypes, modifiers: null);
        if (parameterlessConstructor is not null)
            return _ => parameterlessConstructor.Invoke(null);

        throw new NotSupportedException(
            $"Sequential collection type '{type}' has no constructor that can create a valid empty clone.");
    }

    private static Action<object, object?> CompileCollectionInsertion(
        Type collectionBase,
        Type elementType,
        string methodName)
    {
        MethodInfo insertionMethod = collectionBase.GetMethod(methodName, [elementType])!;
        ParameterExpression collection = Expression.Parameter(typeof(object), "collection");
        ParameterExpression item = Expression.Parameter(typeof(object), "item");
        MethodCallExpression call = Expression.Call(
            Expression.Convert(collection, collectionBase),
            insertionMethod,
            Expression.Convert(item, elementType));

        return Expression.Lambda<Action<object, object?>>(
            Expression.Block(call, Expression.Empty()),
            collection,
            item).Compile();
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
        ClonePopulateDelegate populate = (source, destination, context) =>
        {
            foreach (FieldCopyStep step in steps)
                step(destination, source, context);
        };

        return (source, context) =>
        {
            // Allocate without calling any constructor — no parameterless ctor required.
            // NOTE: field initializers and [OnDeserializing] callbacks do not run here;
            //       this matches FastCloner semantics.
            object dest = RuntimeHelpers.GetUninitializedObject(type);

            // Register BEFORE processing fields so that back-edges in the graph
            // resolve to this (not-yet-finished) clone rather than re-entering.
            context.Register(source, dest);
            context.PopulateOrDefer(source, dest, populate);

            return dest;
        };
    }

    // FieldCopyStep: (destination, source, context) → void
    private delegate void FieldCopyStep(object dest, object source, CloneContext context);

    private static FieldCopyStep[] BuildFieldCopySteps(Type type, Type? stopExclusive = null)
    {
        var steps = new List<FieldCopyStep>();

        foreach (FieldInfo field in GetAllInstanceFields(type, stopExclusive))
        {
            // Capture locally for closure correctness.
            FieldInfo f = field;
            CloneBehavior behavior = CloneTypePolicy.GetFieldBehavior(f);

            if (behavior == CloneBehavior.Ignore)
            {
                object? defaultValue = GetDefaultValue(f.FieldType);
                steps.Add((dest, _, _) => f.SetValue(dest, defaultValue));
                continue;
            }

            bool shallow = behavior == CloneBehavior.Reference;

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
        CloneBehavior[] behaviors = Array.ConvertAll(allFields, CloneTypePolicy.GetFieldBehavior);

        return (source, context) =>
        {
            // GetUninitializedObject returns a boxed zero-initialized value type.
            object dest = RuntimeHelpers.GetUninitializedObject(type);

            // Boxed value types can participate in object-reference cycles.
            // Register the source box before cloning fields so back-edges resolve to dest.
            context.Register(source, dest);

            for (int i = 0; i < allFields.Length; i++)
            {
                if (behaviors[i] == CloneBehavior.Ignore)
                    continue;

                object? value = allFields[i].GetValue(source);
                allFields[i].SetValue(
                    dest,
                    behaviors[i] == CloneBehavior.Reference ? value : CloneObject(value, context));
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
        bool shallowElements = CloneTypePolicy.GetBehavior(elementType) == CloneBehavior.Reference;

        ClonePopulateDelegate populate = (source, destination, context) =>
        {
            var srcArray = (Array)source;
            var destArray = (Array)destination;

            if (shallowElements)
            {
                Array.Copy(srcArray, destArray, srcArray.Length);
                return;
            }

            if (rank == 1)
            {
                int lowerBound = srcArray.GetLowerBound(0);
                int length = srcArray.GetLength(0);
                for (int i = lowerBound; i < lowerBound + length; i++)
                    destArray.SetValue(CloneObject(srcArray.GetValue(i), context), i);
                return;
            }

            int[] lengths = new int[rank];
            int[] lowerBounds = new int[rank];
            for (int dimension = 0; dimension < rank; dimension++)
            {
                lengths[dimension] = srcArray.GetLength(dimension);
                lowerBounds[dimension] = srcArray.GetLowerBound(dimension);
            }

            CopyMultiDimArray(srcArray, destArray, rank, lengths, lowerBounds, context);
        };

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
            context.PopulateOrDefer(source, destArray, populate);

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
    private static IEnumerable<FieldInfo> GetAllInstanceFields(Type type, Type? stopExclusive = null)
    {
        // Build base-first list so fields are processed in hierarchy order.
        var hierarchy = new Stack<Type>();
        Type? t = type;
        while (t is not null && t != typeof(object) && t != stopExclusive)
        {
            hierarchy.Push(t);
            t = t.BaseType;
        }

        while (hierarchy.TryPop(out Type? current))
            foreach (FieldInfo field in current.GetFields(InstanceAllDeclared))
                yield return field;
    }

    private static Type? FindConstructedGenericBase(Type type, Type genericDefinition)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericDefinition)
                return current;
        }

        return null;
    }

    private static object? GetDefaultValue(Type type)
        => type.IsValueType ? Array.CreateInstance(type, 1).GetValue(0) : null;

}
