using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class QueueingRuntimeLink<TSource, TValue>(ILink<TValue> link, object? value) :
    RuntimeLink<TSource, TValue>(link, value), IQueueingRuntimeLink<TValue>
{
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
        lock (_queueLock)
            _queue.Enqueue(convertedValue);
        SetConvertedData(convertedValue, changedTime, validity);
    }

    public void StoreValue(TValue? value)
        => Value = value;
}
