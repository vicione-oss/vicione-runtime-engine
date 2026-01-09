using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

[DebuggerDisplay("{Design.Name,nq}")]
public class ConnectorInput<TValue> : Connector, IConnector<TValue>, IPoolable
{
    public IAggregatingPooling<TValue>? CrossSourceAggregatingPooling { get; set; }
    IAggregatingPooling? IPoolable.CrossSourceAggregatingPooling => CrossSourceAggregatingPooling;
    public new ConnectorDesignInput<TValue> Design
    {
        get => (ConnectorDesignInput<TValue>)base.Design;
        set => base.Design = value;
    }
    IConnectorDesign<TValue> IConnector<TValue>.Design => Design;
    public PoolingStrategy CrossSourcePoolingStrategy { get; set; }
    public IAggregatingPooling<TValue>? PerSourceAggregatingPooling { get; set; }
    IAggregatingPooling? IPoolable.PerSourceAggregatingPooling => PerSourceAggregatingPooling;
    public PoolingStrategy PerSourcePoolingStrategy { get; set; }
    public new List<IValidator<TValue>> Validators { get; set; } = [];
    IEnumerable<IValidator<TValue>> IConnector<TValue>.Validators => Validators;
    public new TValue? Value
    {
        get => (TValue?)base.Value;
        set => base.Value = value;
    }

    public ConnectorInput()
    {
        Design = new ConnectorDesignInput<TValue>();
        Value = default;
    }

    private protected override IEnumerable<IValidator> GetValidators() => Validators;
}
