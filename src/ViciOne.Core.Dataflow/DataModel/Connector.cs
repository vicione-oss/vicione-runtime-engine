using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class Connector : IConnector
{
    public ConnectorDesign Design { get; set; } = EmptyConnectorDesign.Default;
    IConnectorDesign IConnector.Design => Design;
    public Guid Id { get; set; }
    public bool MarkAsChangedOnlyIfNotEqual { get; set; }
    public object? Value { get; set; }
    public IEnumerable<IValidator> Validators => GetValidators();

    private protected abstract IEnumerable<IValidator> GetValidators();
}
