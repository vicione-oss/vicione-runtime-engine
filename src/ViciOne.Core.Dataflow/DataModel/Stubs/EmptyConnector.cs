using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Stubs;

internal sealed class EmptyConnector : Connector
{
    public static EmptyConnector Default { get; } = new();

    private protected override IEnumerable<IValidator> GetValidators() => [];
}
