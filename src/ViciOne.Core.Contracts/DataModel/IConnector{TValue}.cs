using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IConnector<TValue> : IConnector
{
    new IConnectorDesign<TValue> Design { get; }
    new IEnumerable<IValidator<TValue>> Validators { get; }
    new TValue? Value { get; set; }
}
