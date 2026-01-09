using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IConnector : IIdProvider
{
    IConnectorDesign Design { get; }
    bool MarkAsChangedOnlyIfNotEqual { get; }
    IEnumerable<IValidator> Validators { get; }
    object? Value { get; set; }
}
