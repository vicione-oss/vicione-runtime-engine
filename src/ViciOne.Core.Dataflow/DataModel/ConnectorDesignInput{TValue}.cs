using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;

namespace ViciOne.Core.Dataflow.DataModel;

[DebuggerDisplay("{Name,nq}")]
public class ConnectorDesignInput<TValue> : ConnectorDesign, IConnectorDesign<TValue>, IPoolableDesign
{
    public override Type ConnectorEditorType => typeof(ConnectorInputEditor<TValue>);
    public override Type ConnectorType => typeof(ConnectorInput<TValue>);
    public IAggregatingPooling<TValue>? DefaultCrossSourceAggregatingPooling { get; set; }
    IAggregatingPooling? IPoolableDesign.DefaultCrossSourceAggregatingPooling => DefaultCrossSourceAggregatingPooling;
    public PoolingStrategy DefaultCrossSourcePoolingStrategy { get; set; }
    public IAggregatingPooling<TValue>? DefaultPerSourceAggregatingPooling { get; set; }
    IAggregatingPooling? IPoolableDesign.DefaultPerSourceAggregatingPooling => DefaultPerSourceAggregatingPooling;
    public PoolingStrategy DefaultPerSourcePoolingStrategy { get; set; }
    public new TValue? DefaultValue
    {
        get => (TValue?)base.DefaultValue;
        set => base.DefaultValue = value;
    }
    public new List<IValidator<TValue>> Validators { get; set; } = [];
    IEnumerable<IValidator<TValue>> IConnectorDesign<TValue>.Validators => Validators;

    public ConnectorDesignInput()
        => DefaultValue = default;

    private protected override IEnumerable<IValidator> GetValidators() => Validators;
}
