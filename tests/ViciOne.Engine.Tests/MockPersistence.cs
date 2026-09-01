using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1070:Do not declare event fields as virtual", Justification = "The class serves solely as a proxy for using 'AllValuesToPersistRequested' via NSubstitute.")]
public abstract class MockPersistence : IPersistence
{
    public virtual event Func<IEnumerable<IPersistenceEntry>>? AllValuesToPersistRequested;
#pragma warning disable 67 // The event 'EntriesLoaded' is never used
    public virtual event Action<IEnumerable<IPersistenceEntry>>? EntriesLoaded;
#pragma warning restore 67

    public virtual void Persist(IEnumerable<IPersistenceEntry> entries) { }

    public virtual IEnumerable<IPersistenceEntry> RequestAllValues()
        => AllValuesToPersistRequested?.Invoke() ?? [];
}
