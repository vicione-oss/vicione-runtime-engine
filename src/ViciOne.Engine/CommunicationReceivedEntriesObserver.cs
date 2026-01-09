using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class CommunicationReceivedEntriesObserver : IDisposable
{
    private readonly ICommunicationManager _communicationManager;
    private bool _disposed;
    private readonly List<ILinkChangedEntry> _receivedEntries = [];

    public CommunicationReceivedEntriesObserver(ICommunicationManager communicationManager)
    {
        _communicationManager = communicationManager;
        _communicationManager.ChangedEntriesReceived += ChangedEntriesReceived;
    }

    private void ChangedEntriesReceived(object? sender, IEnumerable<ILinkChangedEntry> entries)
    {
        lock (_receivedEntries)
            _receivedEntries.AddRange(entries);
    }

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _communicationManager.ChangedEntriesReceived -= ChangedEntriesReceived;
            _receivedEntries.Clear();
        }

        _disposed = true;
    }

    public IEnumerable<ILinkChangedEntry> GetAndResetReceivedEntries()
    {
        List<ILinkChangedEntry> receivedEntriesCopy;
        lock (_receivedEntries)
        {
            receivedEntriesCopy = [.. _receivedEntries];
            _receivedEntries.Clear();
        }
        return receivedEntriesCopy;
    }
}
