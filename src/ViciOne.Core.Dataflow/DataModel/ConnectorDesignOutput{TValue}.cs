using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;

namespace ViciOne.Core.Dataflow.DataModel;

[DebuggerDisplay("{Name,nq}")]
public class ConnectorDesignOutput<TValue> : ConnectorDesign, IConnectorDesign<TValue>
{
    public override Type ConnectorEditorType => typeof(ConnectorOutputEditor<TValue>);
    public override Type ConnectorType => typeof(ConnectorOutput<TValue>);
    public new TValue? DefaultValue
    {
        get => (TValue?)base.DefaultValue;
        set => base.DefaultValue = value;
    }
    public new List<IValidator<TValue>> Validators { get; set; } = [];
    IEnumerable<IValidator<TValue>> IConnectorDesign<TValue>.Validators => Validators;

    public ConnectorDesignOutput()
        => DefaultValue = default;

    private protected override IEnumerable<IValidator> GetValidators() => Validators;
}
