using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

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
            return _queue.GetAllAndClear();
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
            EngineTelemetry.LinkQueuesOverflowCount.Add(1);
        }

        SetConvertedData(convertedValue, changedTime, validity);
    }

    public void StoreValue(TValue? value)
        => Value = value;
}
