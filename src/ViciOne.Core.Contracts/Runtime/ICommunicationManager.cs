using System;
using System.Collections.Generic;

namespace ViciOne.Core.Contracts.Runtime;

public interface ICommunicationManager
{
    event EventHandler<IEnumerable<ILinkChangedEntry>>? ChangedEntriesReceived;

    void HandleChangedOutgoingLinks(IEnumerable<ILinkChangedEntry> externalLinkEntries);
}
