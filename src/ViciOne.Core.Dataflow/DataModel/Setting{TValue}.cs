using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class Setting<TValue> : Setting, ISetting<TValue>
{
    public new SettingDesign<TValue> Design
    {
        get => (SettingDesign<TValue>)base.Design;
        set => base.Design = value;
    }
    ISettingDesign<TValue> ISetting<TValue>.Design => Design;
    public new ConnectorInput<TValue> Input
    {
        get => (ConnectorInput<TValue>)base.Input;
        set => base.Input = value;
    }
    IConnector<TValue> ISetting<TValue>.Input => Input;
    public new ConnectorOutput<TValue> Output
    {
        get => (ConnectorOutput<TValue>)base.Output;
        set => base.Output = value;
    }
    IConnector<TValue> ISetting<TValue>.Output => Output;
    public new TValue? Value
    {
        get => (TValue?)base.Value;
        set => base.Value = value;
    }

    public Setting()
    {
        Design = new SettingDesign<TValue>();
        Input = new ConnectorInput<TValue>();
        Output = new ConnectorOutput<TValue>();
        Value = default;
    }
}
