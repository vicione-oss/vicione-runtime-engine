using System;
using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel;

public interface ISettingDesign : IIdProvider
{
    object? DefaultValue { get; }
    Type SettingEditorType { get; }
    Type SettingType { get; }
    IConnectorDesign Input { get; }
    string Name { get; }
    IConnectorDesign Output { get; }
    IEnumerable<IValidator> Validators { get; }
}
