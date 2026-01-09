using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;

namespace ViciOne.Core.Dataflow.DataModel;

public class SettingDesign<TValue> : SettingDesign, ISettingDesign<TValue>
{
    public new TValue? DefaultValue
    {
        get => (TValue?)base.DefaultValue;
        set => base.DefaultValue = value;
    }
    object? ISettingDesign.DefaultValue => DefaultValue;
    public new ConnectorDesignInput<TValue> Input
    {
        get => (ConnectorDesignInput<TValue>)base.Input;
        set => base.Input = value;
    }
    IConnectorDesign<TValue> ISettingDesign<TValue>.Input => Input;
    public new ConnectorDesignOutput<TValue> Output
    {
        get => (ConnectorDesignOutput<TValue>)base.Output;
        set => base.Output = value;
    }
    IConnectorDesign<TValue> ISettingDesign<TValue>.Output => Output;
    public override Type SettingEditorType => typeof(SettingEditor<TValue>);
    public override Type SettingType => typeof(Setting<TValue>);
    public new List<IValidator<TValue>> Validators { get; set; } = [];
    IEnumerable<IValidator<TValue>> ISettingDesign<TValue>.Validators => Validators;

    public SettingDesign()
    {
        DefaultValue = default;
        Input = new ConnectorDesignInput<TValue>();
        Output = new ConnectorDesignOutput<TValue>();
    }

    private protected override IEnumerable<IValidator> GetValidators() => Validators;
}
