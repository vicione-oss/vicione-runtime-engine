namespace ViciOne.Core.Contracts.DataModel;

public interface IPoolable
{
    IAggregatingPooling? CrossSourceAggregatingPooling { get; }
    PoolingStrategy CrossSourcePoolingStrategy { get; }
    IAggregatingPooling? PerSourceAggregatingPooling { get; }
    PoolingStrategy PerSourcePoolingStrategy { get; }
}
