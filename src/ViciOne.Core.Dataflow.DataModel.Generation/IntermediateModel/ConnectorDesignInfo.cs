using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;

public sealed class ConnectorDesignInfo
{
    public IAggregatingPooling? DefaultCrossSourceAggregatingPooling { get; set; }
    public PoolingStrategy? DefaultCrossSourcePoolingStrategy { get; set; }
    public IAggregatingPooling? DefaultPerSourceAggregatingPooling { get; set; }
    public PoolingStrategy? DefaultPerSourcePoolingStrategy { get; set; }
    public object? DefaultValue { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
    public IValidator[] Validators { get; set; } = [];
}
