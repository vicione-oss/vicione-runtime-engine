using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

namespace ViciOne.Engine;

internal sealed class QueueingIncomingInputLinkGroup<TValue> : IncomingInputLinkGroup<TValue, TValue>, IDisposable
{
    private bool _disposed;
    private long _entriesDropped;
    private readonly Queue<TValue?> _queue = new();
    private readonly int _queueCapacity;
    private int _queueCount;
    private readonly Lock _queueLock = new();
    private readonly Func<IPoolingValidator> _getPoolingValidator;
    private readonly ILogger _logger;

    private bool IsAnyLinkChanged
    {
        get
        {
            foreach (IRuntimeLink<TValue> link in Links)
                if (link.Changed)
                    return true;
            return false;
        }
    }

    public override bool NeedsFurtherProcessing =>
        Volatile.Read(ref _queueCount) > 0 || // Stale read is still possible but only delays processing by one engine cycle — acceptable.
        (PerSourceAggregatingPooling is not null && IsAnyLinkChanged);

    public QueueingIncomingInputLinkGroup(IRuntimeConnector<TValue> connector, IRuntimeLink<TValue>[] links, Func<IPoolingValidator> getPoolingValidator, int queueCapacityPerLink, ILogger logger)
        : base(connector, links)
    {
        _getPoolingValidator = getPoolingValidator;
        _queueCapacity = queueCapacityPerLink * links.Length;
        _logger = logger;

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
        bool droppedEntry = false;
        long currentEntriesDropped = 0;
        lock (_queueLock)
        {
            if (_disposed)
                return;

            _queue.Enqueue(item, out _queueCount);
            if (_queue.Count > _queueCapacity)
            {
                _queue.Dequeue(out _queueCount);
                droppedEntry = true;
                currentEntriesDropped = ++_entriesDropped;
            }
        }

        if (droppedEntry)
        {
            _logger.CrossSourceQueueOverflow(_queueCapacity, currentEntriesDropped, nameof(EngineQueueOptions.CrossSourceQueueCapacityPerLink));
            EngineTelemetry.CrossSourceQueuesOverflowCount.Add(1);
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
        {
            IPoolingValidator validator = _getPoolingValidator();
            foreach (IRuntimeLink<TValue> link in validLinks)
            {
                if (link is not IQueueingRuntimeLink<TValue> queueingLink)
                    continue;

                TValue?[] queuedValues = queueingLink.GetAndClearQueuedValues();
                if (queuedValues.Length > 0)
                {
                    queueingLink.StoreValue(PerSourceAggregatingPooling.Aggregate(queuedValues));
                    LinkChanged(queueingLink, validator);
                }
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
