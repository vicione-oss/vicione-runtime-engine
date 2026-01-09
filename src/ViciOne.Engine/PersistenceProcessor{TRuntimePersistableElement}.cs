using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Vorlage muss ein statischer Ausdruck sein", Justification = "Der abgeleitete Typ stellt über die Eigenschaften passende statische Vorlagen bereit.")]
internal abstract class PersistenceProcessor<TPersistableElementProvider, TRuntimePersistableElement> : IDisposable
    where TPersistableElementProvider : IChangingElementsProvider<TRuntimePersistableElement>, IEnumerable<TRuntimePersistableElement>
    where TRuntimePersistableElement : IRuntimePersistable
{
    private bool _disposed;
    private readonly List<(TRuntimePersistableElement, IPersistenceEntry)> _elementsToApply = [];
    private readonly Func<IReferenceTypeCloneHandler> _getCloneHandler;
    private readonly IPersistence _persistence;
    private readonly PersistenceEntryFactory _persistenceEntryFactory = new();

    protected abstract RuntimeElementsChangedObserver<TPersistableElementProvider, TRuntimePersistableElement> ChangedObserver { get; }
    protected abstract RuntimeElementsProvider<TPersistableElementProvider, TRuntimePersistableElement> ElementProvider { get; }

    public PersistenceProcessor(IPersistence persistence, Func<IReferenceTypeCloneHandler> getCloneHandler)
    {
        _persistence = persistence;
        _getCloneHandler = getCloneHandler;
        AttachToPersistence();
    }

    public void ApplyLoaded()
    {
        var elementsToApply = new List<(TRuntimePersistableElement, IPersistenceEntry)>();
        lock (_elementsToApply)
        {
            elementsToApply.AddRange(_elementsToApply);
            _elementsToApply.Clear();
        }

        foreach ((TRuntimePersistableElement element, IPersistenceEntry loadedEntry) in elementsToApply)
            try { element.ApplyLoadedValue(loadedEntry.Value, loadedEntry.Time); }
            catch (Exception ex) { LogErrorWhileApplying(ex, loadedEntry.Id, element); }
    }

    private void AttachToPersistence()
    {
        _persistence.AllValuesToPersistRequested += Persistence_AllValuesToPersistRequested;
        _persistence.EntriesLoaded += Persistence_EntriesLoaded;
    }

    private IPersistenceEntry CreatePersistenceEntry(TRuntimePersistableElement element)
        => _persistenceEntryFactory.Create(element, _getCloneHandler());

    private void DetachFromPersistence()
    {
        _persistence.AllValuesToPersistRequested -= Persistence_AllValuesToPersistRequested;
        _persistence.EntriesLoaded -= Persistence_EntriesLoaded;
    }

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            DetachFromPersistence();
            lock (_elementsToApply)
                _elementsToApply.Clear();
            _persistenceEntryFactory.Dispose();
            ChangedObserver.Dispose();
            ElementProvider.Dispose();
        }

        _disposed = true;
    }

    protected abstract void LogErrorWhileApplying(Exception ex, Guid loadedId, IIdProvider element);
    protected abstract void LogWarningVariableNotFound(Guid loadedId);

    public void PersistChanged()
    {
        IEnumerable<IPersistenceEntry> changedEntries = [.. ChangedObserver.GetAndResetChanged().Values
            .SelectMany(x => x)
            .Select(CreatePersistenceEntry)];
        _persistence.Persist(changedEntries);
    }

    private IPersistenceEntry[] Persistence_AllValuesToPersistRequested()
        => [.. ElementProvider.GetAll().Values
            .SelectMany(x => x)
            .Select(CreatePersistenceEntry)];

    private void Persistence_EntriesLoaded(IEnumerable<IPersistenceEntry> loadedEntries)
    {
        var elements = ElementProvider.GetAll().Values
            .SelectMany(x => x)
            .ToDictionary(
                x => x.Id,
                x => x);
        foreach (IPersistenceEntry loadedEntry in loadedEntries)
        {
            if (elements.TryGetValue(loadedEntry.Id, out TRuntimePersistableElement? element) == false)
            {
                LogWarningVariableNotFound(loadedEntry.Id);
                continue;
            }

            lock (_elementsToApply)
                _elementsToApply.Add((element, loadedEntry));
        }
    }
}
