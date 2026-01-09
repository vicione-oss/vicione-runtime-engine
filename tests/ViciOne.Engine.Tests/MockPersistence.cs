using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1070:Ereignisfelder nicht als virtuell deklarieren", Justification = "Die Klasse dient ausschliesslich als Proxy zur Nutzung von 'AllValuesToPersistRequested' via NSubstitute.")]
public abstract class MockPersistence : IPersistence
{
    public virtual event Func<IEnumerable<IPersistenceEntry>>? AllValuesToPersistRequested;
#pragma warning disable 67 // Das Ereignis 'EntriesLoaded' wird nie verwendet
    public virtual event Action<IEnumerable<IPersistenceEntry>>? EntriesLoaded;
#pragma warning restore 67

    public virtual void Persist(IEnumerable<IPersistenceEntry> entries) { }

    public virtual IEnumerable<IPersistenceEntry> RequestAllValues()
        => AllValuesToPersistRequested?.Invoke() ?? [];
}
