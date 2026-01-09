using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class ConnectorDesign : IConnectorDesign
{
    public abstract Type ConnectorEditorType { get; }
    public abstract Type ConnectorType { get; }
    public object? DefaultValue { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IEnumerable<IValidator> Validators => GetValidators();

    private protected abstract IEnumerable<IValidator> GetValidators();
}
