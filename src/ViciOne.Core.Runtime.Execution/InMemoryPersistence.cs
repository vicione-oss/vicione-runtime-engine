using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Execution;

internal sealed class InMemoryPersistence : IPersistence, IDisposable
{
    private bool _disposed;
    private readonly List<IPersistenceEntry> _entries = [];

    public event Func<IEnumerable<IPersistenceEntry>>? AllValuesToPersistRequested;
    public event EventHandler? EntriesAdded;
    public event Action<IEnumerable<IPersistenceEntry>>? EntriesLoaded;

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _entries.Clear();

        _disposed = true;
    }

    public IPersistenceEntry[] GetAndResetPersisted()
    {
        lock (_entries)
        {
            IPersistenceEntry[] entries = [.. _entries];
            _entries.Clear();
            return entries;
        }
    }

    public IPersistenceEntry[] GetPersisted()
    {
        lock (_entries)
            return [.. _entries];
    }

    public void Persist(IEnumerable<IPersistenceEntry> entries)
    {
        if (entries.Any() == false)
            return;
        lock (_entries)
            _entries.AddRange(entries);
        EntriesAdded?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseEntriesLoaded(IEnumerable<IPersistenceEntry> entries)
        => EntriesLoaded?.Invoke(entries);

    public IPersistenceEntry[] RequestAllValues()
        => AllValuesToPersistRequested?.Invoke()?.ToArray() ?? [];
}
