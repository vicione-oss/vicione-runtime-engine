using System;
using System.Collections.Concurrent;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public class DefaultConnectorValueTransferInterceptor : IConnectorValueTransferInterceptor, IDisposable
{
    private bool _disposed;

    protected ConcurrentDictionary<Guid, object> Interceptors { get; } = [];
    // HACK: Da es kein ConcurrentHashSet gibt, nutzen wir ein Dictionary, bei dem der Wert egal ist.
    protected ConcurrentDictionary<Guid, object?> PendingInterceptions { get; } = [];

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
        {
            Interceptors.Clear();
            PendingInterceptions.Clear();
        }

        _disposed = true;
    }

    public virtual IConnectorValueTransferInterceptionResult<T> Intercept<T>(Guid connectorId, T? value)
    {
        PendingInterceptions.TryRemove(connectorId, out _);
        return Interceptors.TryGetValue(connectorId, out object? interceptorFunc)
            ? ((Func<T?, IConnectorValueTransferInterceptionResult<T>>)interceptorFunc)(value)
            : new DefaultConnectorValueTransferInterceptionResult<T>() { Value = value };
    }

    public virtual bool IsInterceptionPending(Guid id)
        => PendingInterceptions.ContainsKey(id);

    public virtual void RegisterInterceptor<T>(Guid connectorId, Func<T?, IConnectorValueTransferInterceptionResult<T>> interceptorFunc)
        => Interceptors[connectorId] = interceptorFunc;

    public virtual void SignalInterceptionPending(Guid connectorId)
    {
        if (Interceptors.ContainsKey(connectorId))
            PendingInterceptions[connectorId] = default;
    }

    public virtual void UnregisterInterceptor(Guid connectorId)
    {
        if (Interceptors.TryRemove(connectorId, out _))
            PendingInterceptions[connectorId] = default;
    }
}
