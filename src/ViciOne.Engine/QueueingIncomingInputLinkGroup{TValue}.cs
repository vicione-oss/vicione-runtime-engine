using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

namespace ViciOne.Engine;

internal sealed class QueueingIncomingInputLinkGroup<TValue> : IncomingInputLinkGroup<TValue, TValue>, IDisposable
{
    // TODO: Soll das konfigurierbar sein? Falls ja, wie?
    internal const int MaximumCapacityPerLink = 100;

    private bool _disposed;
    private readonly int _maximumCapacity;
    private readonly Queue<TValue?> _queue = new();
    private int _queueCount;
    private readonly Lock _queueLock = new();
    private readonly Func<IPoolingValidator> _getPoolingValidator;

    public override bool NeedsFurtherProcessing =>
        Volatile.Read(ref _queueCount) > 0 || // Stale read is still possible but only delays processing by one engine cycle — acceptable.
        (PerSourceAggregatingPooling is not null && Links.Any(x => x.Changed));

    public QueueingIncomingInputLinkGroup(IRuntimeConnector<TValue> connector, IRuntimeLink<TValue>[] links, Func<IPoolingValidator> getPoolingValidator) : base(connector, links)
    {
        _getPoolingValidator = getPoolingValidator;
        _maximumCapacity = MaximumCapacityPerLink * links.Length;

        if (PerSourceAggregatingPooling is null)
            foreach (IRuntimeLink<TValue> link in links)
                link.LinkChanged += LinkChanged;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (PerSourceAggregatingPooling is null)
            foreach (IRuntimeLink<TValue> link in Links)
                link.LinkChanged -= LinkChanged;

        lock (_queueLock)
        {
            _queue.Clear();
            _queueCount = 0;
        }
    }

    private void Enqueue(TValue? item)
    {
        lock (_queueLock)
        {
            if (_disposed)
                return;

            _queue.Enqueue(item, out _queueCount);
            if (_queue.Count > _maximumCapacity)
                _queue.Dequeue(out _queueCount);
        }
    }

    private void LinkChanged(object? sender, EventArgs e)
        => LinkChanged((IRuntimeLink<TValue>)sender!, _getPoolingValidator());

    private void LinkChanged(IRuntimeLink<TValue> link, IPoolingValidator validator)
    {
        if (validator.IsLinkValid(link))
            Enqueue(link.Value);
        link.GetAndResetChanged();
    }

    protected override TValue? PoolValidLinks(IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
    {
        if (PerSourceAggregatingPooling is not null)
            foreach (IQueueingRuntimeLink<TValue> link in validLinks.OfType<IQueueingRuntimeLink<TValue>>())
            {
                TValue?[] queuedValues = link.GetAndClearQueuedValues();
                if (queuedValues.Length > 0)
                {
                    link.StoreValue(PerSourceAggregatingPooling.Aggregate(queuedValues));
                    LinkChanged(link, _getPoolingValidator());
                }
            }

        lock (_queueLock)
        {
            createdValue = _queue.Count > 0;
            return createdValue
                ? _queue.Dequeue(out _queueCount)
                : default;
        }
    }
}
