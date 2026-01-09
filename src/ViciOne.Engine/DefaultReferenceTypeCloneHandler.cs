using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public class DefaultReferenceTypeCloneHandler : IReferenceTypeCloneHandler, IDisposable
{
    private bool _disposed;

    protected Dictionary<Type, object> CloneHandlers { get; } = [];

    public virtual T? CloneIfNecessary<T>(T? value)
        => CloneHandlers.TryGetValue(typeof(T), out object? cloneHandler) ? ((Func<T?, T?>)cloneHandler)(value) : value;

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
            CloneHandlers.Clear();

        _disposed = true;
    }

    public virtual void RegisterHandler<T>(Func<T?, T?> cloneFunc) where T : class
        => CloneHandlers[typeof(T)] = cloneFunc;
}
