using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime.Execution;

internal sealed class InMemoryCommunicationManager : ICommunicationManager, IDisposable
{
    private bool _disposed;
    private readonly List<ILinkChangedEntry> _handledExternalOutgoingLinkChangedEntries = [];

    public event EventHandler<IEnumerable<ILinkChangedEntry>>? ChangedEntriesReceived;
    public event EventHandler? HandledExternalOutgoingLinkChangedEntriesChanged;

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _handledExternalOutgoingLinkChangedEntries.Clear();

        _disposed = true;
    }

    public IEnumerable<ILinkChangedEntry> GetAndResetHandledExternalOutgoingLinkChangedEntries()
    {
        lock (_handledExternalOutgoingLinkChangedEntries)
        {
            ILinkChangedEntry[] entries = [.. _handledExternalOutgoingLinkChangedEntries];
            _handledExternalOutgoingLinkChangedEntries.Clear();
            return entries;
        }
    }

    public void HandleChangedOutgoingLinks(IEnumerable<ILinkChangedEntry> externalLinkEntries)
    {
        if (externalLinkEntries.Any() == false)
            return;
        lock (_handledExternalOutgoingLinkChangedEntries)
            _handledExternalOutgoingLinkChangedEntries.AddRange(externalLinkEntries);
        HandledExternalOutgoingLinkChangedEntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseChangedEntriesReceived(IEnumerable<ILinkChangedEntry> entries)
        => ChangedEntriesReceived?.Invoke(this, entries);
}
