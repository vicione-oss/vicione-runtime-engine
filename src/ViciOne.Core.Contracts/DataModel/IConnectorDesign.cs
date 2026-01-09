using System;
using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IConnectorDesign : IIdProvider
{
    Type ConnectorEditorType { get; }
    Type ConnectorType { get; }
    object? DefaultValue { get; }
    string Name { get; }
    IEnumerable<IValidator> Validators { get; }
}
