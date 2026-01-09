using System;
using System.Collections.Generic;

namespace ViciOne.Core.Contracts.Runtime;

public interface IPersistence
{
    event Func<IEnumerable<IPersistenceEntry>>? AllValuesToPersistRequested;
    event Action<IEnumerable<IPersistenceEntry>>? EntriesLoaded;

    void Persist(IEnumerable<IPersistenceEntry> entries);
}
