using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class CommunicationProcessor(LinkCache linkCache, IUniqueNameProvider uniqueNameProvider, ICommunicationManager communicationManager, int receiveBufferCapacity, ILogger logger) : IDisposable
{
    private bool _disposed;
    private readonly LinkChangedEntryFactory _linkChangedEntryFactory = new();
    private readonly CommunicationReceivedEntriesObserver _receivedEntriesObserver = new(communicationManager, receiveBufferCapacity, logger);

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
        }

        _disposed = true;
    }

    private List<ILinkChangedEntry> GetChangedEntries(IEnumerable<IRuntimeLink> links, IReferenceTypeCloneHandler cloneHandler, Action<IRuntimeLink, Exception> onError)
    {
        var changedEntries = new List<ILinkChangedEntry>();
        // TODO: possibly parallelize
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
            linkCache.GetExternalOutgoingLinks(),
            cloneHandler,
            (link, ex) =>
            {
                var externalOutgoingLink = (IExternalOutgoingLink)link.Link;
                logger.ExternalOutputLinkProcessingErrorOccured(ex, externalOutgoingLink.Id, externalOutgoingLink.Source.Id, uniqueNameProvider.GetUniqueName(externalOutgoingLink.Source));
            });

    public void ProcessInputCommunication()
    {
        // it is assumed that
        // - if the destination (pooling) cannot buffer, only one value per link comes in per cycle
        // - if the destination (pooling) can buffer, multiple values per link can come in per cycle
        // TODO: the behavior described here and implemented below will change as soon as the links each get their own queues

        IEnumerable<ILinkChangedEntry> receivedChangedEntries = _receivedEntriesObserver.GetAndResetReceivedEntries();
        IDictionary<Guid, IRuntimeLink> externalIncomingLinks = linkCache.GetExternalIncomingLinks();

        var entryLinkPairs = new List<(ILinkChangedEntry Entry, IRuntimeLink Link)>();
        bool sortEntries = false;
        var queueableEntries = new HashSet<Guid>();
        foreach (ILinkChangedEntry receivedEntry in receivedChangedEntries)
        {
            if (externalIncomingLinks.TryGetValue(receivedEntry.Id, out IRuntimeLink? link) == false)
            {
                logger.NoLinkFoundForReceivedId(receivedEntry.Id);
                continue;
            }

            // TODO: make this more type-safe... In theory an IRuntimeLink could also represent links without a destination. Here we only know it because _linkCache.GetExternalIncomingLinks() returns only links with a destination
            if (sortEntries == false && ((IPoolable)((ILinkWithDestination)link.Link).Destination).CrossSourcePoolingStrategy == PoolingStrategy.Queue)
            {
                if (queueableEntries.Contains(receivedEntry.Id))
                {
                    // the received entries only need to be sorted if multiple values were received per destination
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
                catch (Exception ex) { logger.TransferReceivedValueToLinkErrorOccured(ex, entry.Id); }
    }

    public void ProcessOutputCommunication(IReferenceTypeCloneHandler cloneHandler)
    {
        List<ILinkChangedEntry> externalLinkEntries = GetExternalOutgoingLinksChangedEntries(cloneHandler);
        if (externalLinkEntries.Count > 0)
            // TODO: try...catch
            // TODO: possibly move to a separate thread
            communicationManager.HandleChangedOutgoingLinks(externalLinkEntries);
    }
}
