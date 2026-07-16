using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

namespace ViciOne.Engine;

internal sealed class CommunicationReceivedEntriesObserver : IDisposable
{
    private readonly int _bufferCapacity;
    private readonly ICommunicationManager _communicationManager;
    private bool _disposed;
    private readonly ILogger _logger;
    private List<ILinkChangedEntry> _receivedEntries = [];
    private long _receivedEntriesDropped;
    private readonly Lock _receivedEntriesLock = new();

    public CommunicationReceivedEntriesObserver(ICommunicationManager communicationManager, int bufferCapacity, ILogger logger)
    {
        _communicationManager = communicationManager;
        _bufferCapacity = bufferCapacity;
        _logger = logger;
        _communicationManager.ChangedEntriesReceived += ChangedEntriesReceived;
    }

    private void ChangedEntriesReceived(object? sender, IEnumerable<ILinkChangedEntry> entries)
    {
        lock (_receivedEntriesLock)
            _receivedEntries.AddRangeWithDropNewest(entries, _bufferCapacity, ref _receivedEntriesDropped);
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
            lock (_receivedEntriesLock)
                _receivedEntries.Clear();
        }

        _disposed = true;
    }

    public IEnumerable<ILinkChangedEntry> GetAndResetReceivedEntries()
    {
        List<ILinkChangedEntry>? receivedEntriesSinceReset = null;
        long droppedEntriesSinceReset;

        lock (_receivedEntriesLock)
        {
            droppedEntriesSinceReset = _receivedEntriesDropped;
            _receivedEntriesDropped = 0;

            if (_receivedEntries.Count > 0)
            {
                receivedEntriesSinceReset = _receivedEntries;
                _receivedEntries = new(receivedEntriesSinceReset.Count); // pre size to avoid the doubling of the internal array when adding entries
            }
        }

        if (droppedEntriesSinceReset > 0)
        {
            _logger.CommunicationReceiveBufferOverflow(droppedEntriesSinceReset, _bufferCapacity, nameof(EngineQueueOptions.CommunicationReceiveBufferCapacity));
            EngineTelemetry.CommunicationReceiveBufferOverflowCount.Add(droppedEntriesSinceReset);
        }

        return receivedEntriesSinceReset ?? [];
    }
}
