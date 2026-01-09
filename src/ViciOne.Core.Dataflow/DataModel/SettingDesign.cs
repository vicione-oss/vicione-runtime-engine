using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class SettingDesign : ISettingDesign
{
    public object? DefaultValue { get; set; }
    public Guid Id { get; set; }
    public ConnectorDesign Input { get; set; } = EmptyConnectorDesign.Default;
    IConnectorDesign ISettingDesign.Input => Input;
    public string Name { get; set; } = string.Empty;
    public ConnectorDesign Output { get; set; } = EmptyConnectorDesign.Default;
    IConnectorDesign ISettingDesign.Output => Output;
    public abstract Type SettingEditorType { get; }
    public abstract Type SettingType { get; }
    public IEnumerable<IValidator> Validators => GetValidators();

    private protected abstract IEnumerable<IValidator> GetValidators();
}
