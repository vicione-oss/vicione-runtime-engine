namespace ViciOne.Core.Contracts.Runtime;

public interface IQueueingRuntimeLink<TValue> : IRuntimeLink<TValue>
{
    bool TryDequeueValue(out TValue? dequeued);
    TValue?[] GetAndClearQueuedValues();
    void StoreValue(TValue? value);
}
