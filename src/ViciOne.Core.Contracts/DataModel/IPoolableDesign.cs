namespace ViciOne.Core.Contracts.DataModel;

public interface IPoolableDesign
{
    IAggregatingPooling? DefaultCrossSourceAggregatingPooling { get; }
    PoolingStrategy DefaultCrossSourcePoolingStrategy { get; }
    IAggregatingPooling? DefaultPerSourceAggregatingPooling { get; }
    PoolingStrategy DefaultPerSourcePoolingStrategy { get; }
}
