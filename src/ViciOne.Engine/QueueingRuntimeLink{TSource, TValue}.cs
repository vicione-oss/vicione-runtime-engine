using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class QueueingRuntimeLink<TSource, TValue>(ILink<TValue> link, object? value, int queueCapacity = QueueingRuntimeLink<TSource, TValue>.DefaultQueueCapacity) :
    RuntimeLink<TSource, TValue>(link, value), IQueueingRuntimeLink<TValue>
{
    private const int DefaultQueueCapacity = 1_000;

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
        lock (_queueLock)
        {
            _queue.Enqueue(convertedValue);
            if (_queue.Count > _queueCapacity)
            {
                _queue.Dequeue();
                droppedEntry = true;
            }
        }

        if (droppedEntry)
        {
            // TODO: Log a warning that an entry was dropped due to exceeding the queue capacity.
            // TODO: Telemetry: Increment a counter for dropped entries.
        }

        SetConvertedData(convertedValue, changedTime, validity);
    }

    public void StoreValue(TValue? value)
        => Value = value;
}
