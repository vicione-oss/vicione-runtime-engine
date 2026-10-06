using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine.Benchmarks;

internal sealed class StubCommunicationManager : ICommunicationManager
{
    public event EventHandler<IEnumerable<ILinkChangedEntry>>? ChangedEntriesReceived;

    public void HandleChangedOutgoingLinks(IEnumerable<ILinkChangedEntry> externalLinkEntries) { }

    public void RaiseChangedEntriesReceived(IEnumerable<ILinkChangedEntry> entries)
        => ChangedEntriesReceived?.Invoke(this, entries);
}
