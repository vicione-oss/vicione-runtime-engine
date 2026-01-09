using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface IConnectorDesign<TValue> : IConnectorDesign
{
    new TValue? DefaultValue { get; }
    new IEnumerable<IValidator<TValue>> Validators { get; }
}
