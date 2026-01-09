using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class SettingEditor<TValue> : ISettingEditor
{
    private readonly ConnectorInputEditor<TValue> _inputEditor;
    private readonly ConnectorOutputEditor<TValue> _outputEditor;

    public Setting<TValue> Setting { get; set; }
    Setting ISettingEditor.Setting => Setting;

    public SettingEditor(SettingDesign<TValue> design)
        => Setting = CreateModel(design, out _inputEditor, out _outputEditor);

    public SettingEditor(Setting<TValue> setting)
    {
        Setting = setting;
        _inputEditor = new ConnectorInputEditor<TValue>(Setting.Input);
        _outputEditor = new ConnectorOutputEditor<TValue>(Setting.Output);
    }

    protected static Setting<TValue> CreateModel(SettingDesign<TValue> design,
        out ConnectorInputEditor<TValue> inputEditor, out ConnectorOutputEditor<TValue> outputEditor)
    {
        inputEditor = new ConnectorInputEditor<TValue>(design.Input);
        outputEditor = new ConnectorOutputEditor<TValue>(design.Output);

        var setting = (Setting<TValue>)Activator.CreateInstance(design.SettingType)!;
        setting.Design = design;
        setting.Id = Guid.NewGuid();
        setting.Value = setting.Design.DefaultValue;
        setting.Input = inputEditor.Connector;
        setting.Output = outputEditor.Connector;
        return setting;
    }

    public virtual void ResetToDefaults()
        => SetValue(Setting.Design.DefaultValue, true);

    public virtual void SetValue(TValue? value)
        => SetValue(value, true);

    protected virtual void SetValue(TValue? value, bool setChangedTime)
    {
        TValue? result = ValidateValue(value);

        if (setChangedTime && EqualityComparer<TValue?>.Default.Equals(Setting.Value, result) == false)
            Setting.ChangedTime = DateTime.UtcNow;

        Setting.Value = result;
        _inputEditor.SetValue(result);
        _outputEditor.SetValue(result);
    }

    public virtual TValue? ValidateValue(TValue? value)
    {
        foreach (IValidator<TValue> validator in Setting.Design.Validators)
            value = validator.Validate(value, true);
        return value;
    }
}
