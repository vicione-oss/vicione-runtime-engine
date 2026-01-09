using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface ISettingDesign<TValue> : ISettingDesign
{
    new TValue? DefaultValue { get; }
    new IConnectorDesign<TValue> Input { get; }
    new IConnectorDesign<TValue> Output { get; }
    new IEnumerable<IValidator<TValue>> Validators { get; }
}
