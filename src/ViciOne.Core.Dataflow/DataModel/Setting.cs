using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class Setting : ISetting
{
    public DateTime ChangedTime { get; set; }
    public SettingDesign Design { get; set; } = EmptySettingDesign.Default;
    ISettingDesign ISetting.Design => Design;
    public Guid Id { get; set; }
    public Connector Input { get; set; } = EmptyConnector.Default;
    IConnector ISetting.Input => Input;
    public Connector Output { get; set; } = EmptyConnector.Default;
    IConnector ISetting.Output => Output;
    public object? Value { get; set; }
}
