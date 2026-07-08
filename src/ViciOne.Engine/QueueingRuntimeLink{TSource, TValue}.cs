using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class QueueingRuntimeLink<TSource, TValue>(ILink<TValue> link, object? value, int queueCapacity, ILogger logger) :
    RuntimeLink<TSource, TValue>(link, value), IQueueingRuntimeLink<TValue>
{
    private long _entriesDropped;
    private readonly int _queueCapacity = queueCapacity;
    private readonly Queue<TValue?> _queue = new();
    private readonly Lock _queueLock = new();

    public bool TryDequeueValue(out TValue? dequeued)
    {
        lock (_queueLock)
            return _queue.TryDequeue(out dequeued);
    }

    public TValue?[] GetAndClearQueuedValues()
    {
        lock (_queueLock)
        {
            TValue?[] values = [.. _queue];
            _queue.Clear();
            return values;
        }
    }

    public override void SetData(object? value, DateTime changedTime, Validity validity)
    {
        TValue? convertedValue = ConvertValue(value);
        bool droppedEntry = false;
        long currentEntriesDropped = 0;
        lock (_queueLock)
        {
            _queue.Enqueue(convertedValue);
            if (_queue.Count > _queueCapacity)
            {
                _queue.Dequeue();
                droppedEntry = true;
                currentEntriesDropped = ++_entriesDropped;
            }
        }

        if (droppedEntry)
        {
            logger.PerLinkQueueOverflow(_queueCapacity, currentEntriesDropped, nameof(EngineQueueOptions.PerLinkQueueCapacity));
            // TODO: Telemetry: Increment a counter for dropped entries.
        }

        SetConvertedData(convertedValue, changedTime, validity);
    }

    public void StoreValue(TValue? value)
        => Value = value;
}
