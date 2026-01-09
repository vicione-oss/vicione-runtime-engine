using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ViciOne.Engine;

public class GenericFactory : IDisposable
{
    private bool _disposed;
    private readonly MethodInfo _genericCreateMethod;
    private readonly ConcurrentDictionary<Type[], MethodInfo> _typedCreateMethods = new(StructuralEqualityComparer<Type>.Default);

    public GenericFactory(MethodInfo genericCreateMethod)
    {
        if (genericCreateMethod.IsStatic == false)
            throw new ArgumentException($"The supplied create method must be static.", nameof(genericCreateMethod));

        _genericCreateMethod = genericCreateMethod;
    }

    public virtual TReturn Create<TReturn>(Type valueType, object?[] createMethodArgs)
        => Create<TReturn>(createMethodArgs, valueType);

    public virtual TReturn Create<TReturn>(object?[] createMethodArgs, params Type[] valueTypes)
    {
        MethodInfo typedCreateMethod = GetOrCreateTypedCreateMethod(valueTypes);
        try { return (TReturn)typedCreateMethod.Invoke(null, createMethodArgs)!; }
        catch (TargetInvocationException ex)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
            throw; // wird nie erreicht, nur zum Befriedigen des Compilers
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
            _typedCreateMethods.Clear();
        _disposed = true;
    }

    private MethodInfo GetOrCreateTypedCreateMethod(Type[] valueTypes)
    {
        if (_typedCreateMethods.TryGetValue(valueTypes, out MethodInfo? method) == false)
            _typedCreateMethods[valueTypes] = method = _genericCreateMethod.MakeGenericMethod(valueTypes);
        return method;
    }

    private sealed class StructuralEqualityComparer<T> : EqualityComparer<T[]>
    {
        public static new readonly StructuralEqualityComparer<T> Default = new();

        public override bool Equals(T[]? x, T[]? y)
            => StructuralComparisons.StructuralEqualityComparer.Equals(x, y);

        public override int GetHashCode(T[] obj)
            => StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
    }
}
