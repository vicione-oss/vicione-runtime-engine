using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class CommunicationProcessor(LinkCache linkCache, IUniqueNameProvider uniqueNameProvider, ICommunicationManager communicationManager, ILogger logger) : IDisposable
{
    private readonly ICommunicationManager _communicationManager = communicationManager;
    private bool _disposed;
    private readonly LinkCache _linkCache = linkCache;
    private readonly LinkChangedEntryFactory _linkChangedEntryFactory = new();
    private readonly ILogger _logger = logger;
    private readonly CommunicationReceivedEntriesObserver _receivedEntriesObserver = new(communicationManager);
    private readonly IUniqueNameProvider _uniqueNameProvider = uniqueNameProvider;

    private ILinkChangedEntry CreateLinkChangedEntry(IRuntimeLink link, IReferenceTypeCloneHandler cloneHandler)
        => _linkChangedEntryFactory.Create(link, cloneHandler);

    public void Dispose()
        => Dispose(true);

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _receivedEntriesObserver.Dispose();
            _linkChangedEntryFactory.Dispose();
            _linkCache.Dispose();
        }

        _disposed = true;
    }

    private List<ILinkChangedEntry> GetChangedEntries(IEnumerable<IRuntimeLink> links, IReferenceTypeCloneHandler cloneHandler, Action<IRuntimeLink, Exception> onError)
    {
        var changedEntries = new List<ILinkChangedEntry>();
        // TODO: evtl. parallelisieren
        foreach (IRuntimeLink link in links)
            try
            {
                if (link.GetAndResetChanged())
                    changedEntries.Add(CreateLinkChangedEntry(link, cloneHandler));
            }
            catch (Exception ex)
            {
                onError(link, ex);
            }
        return changedEntries;
    }

    private List<ILinkChangedEntry> GetExternalOutgoingLinksChangedEntries(IReferenceTypeCloneHandler cloneHandler)
        => GetChangedEntries(
            _linkCache.GetExternalOutgoingLinks(),
            cloneHandler,
            (link, ex) =>
            {
                var externalOutgoingLink = (IExternalOutgoingLink)link.Link;
                _logger.ExternalOutputLinkProcessingErrorOccured(ex, externalOutgoingLink.Id, externalOutgoingLink.Source.Id, _uniqueNameProvider.GetUniqueName(externalOutgoingLink.Source));
            });

    public void ProcessInputCommunication()
    {
        // es wird davon ausgegangen, dass 
        // - wenn das Ziel(-Pooling) nicht puffern kann, pro Zyklus nur ein Wert pro Link reinkommt
        // - wenn das Ziel(-Pooling) puffern kann, mehrere Werte pro Zyklus pro Link reinkommen können
        // TODO: das hier beschriebene und im Folgenden implementierte Verhalten ändert sich, sobald die Links jeweils eigene Queues erhalten

        IEnumerable<ILinkChangedEntry> receivedChangedEntries = _receivedEntriesObserver.GetAndResetReceivedEntries();
        IDictionary<Guid, IRuntimeLink> externalIncomingLinks = _linkCache.GetExternalIncomingLinks();

        var entryLinkPairs = new List<(ILinkChangedEntry Entry, IRuntimeLink Link)>();
        bool sortEntries = false;
        var queueableEntries = new HashSet<Guid>();
        foreach (ILinkChangedEntry receivedEntry in receivedChangedEntries)
        {
            if (externalIncomingLinks.TryGetValue(receivedEntry.Id, out IRuntimeLink? link) == false)
            {
                _logger.NoLinkFoundForReceivedId(receivedEntry.Id);
                continue;
            }

            // TODO: typsicherer gestalten... Theoretisch könnte ein IRuntimeLink auch Links ohne Ziel abbilden. Hier wissen wir es nur, da _linkCache.GetExternalIncomingLinks() nur Links mit Ziel zurückgibt
            if (sortEntries == false && ((IPoolable)((ILinkWithDestination)link.Link).Destination).CrossSourcePoolingStrategy == PoolingStrategy.Queue)
            {
                if (queueableEntries.Contains(receivedEntry.Id))
                {
                    // die empfangenen Einträge müssen nur sortiert werden, wenn mehrere Werte pro Ziel empfangen wurden
                    sortEntries = true;
                    queueableEntries.Clear();
                }
                else
                    queueableEntries.Add(receivedEntry.Id);
            }

            entryLinkPairs.Add((receivedEntry, link));
        }

        if (sortEntries)
            entryLinkPairs.Sort((x, y) => x.Entry.Time.CompareTo(y.Entry.Time));

        foreach ((ILinkChangedEntry entry, IRuntimeLink link) in entryLinkPairs)
            if (entry.Time > link.ChangedTime)
                try { link.SetData(entry.Value, entry.Time, entry.Validity); }
                catch (Exception ex) { _logger.TransferReceivedValueToLinkErrorOccured(ex, entry.Id); }
    }

    public void ProcessOutputCommunication(IReferenceTypeCloneHandler cloneHandler)
    {
        List<ILinkChangedEntry> externalLinkEntries = GetExternalOutgoingLinksChangedEntries(cloneHandler);
        if (externalLinkEntries.Count > 0)
            // TODO: try...catch
            // TODO: evtl. in eigenen Thread auslagern
            _communicationManager.HandleChangedOutgoingLinks(externalLinkEntries);
    }
}
