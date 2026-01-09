using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Stubs;

internal sealed class EmptyConnectorDesign : ConnectorDesign
{
    public static EmptyConnectorDesign Default { get; } = new();

    public override Type ConnectorEditorType => typeof(object);
    public override Type ConnectorType => typeof(object);

    private protected override IEnumerable<IValidator> GetValidators() => [];
}
