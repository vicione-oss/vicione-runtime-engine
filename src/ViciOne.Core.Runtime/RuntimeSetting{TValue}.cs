using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

public class RuntimeSetting<TValue> : RuntimePersistableElement<ISetting<TValue>, TValue>, IRuntimeSetting<TValue>
{
    public IRuntimeConnector<TValue> Input { get; }
    IRuntimeConnector IRuntimeSetting.Input => Input;
    public IRuntimeConnector<TValue> Output { get; }
    IRuntimeConnector IRuntimeSetting.Output => Output;
    public ISetting<TValue> Setting => Element;
    ISetting IRuntimeSetting.Setting => Element;

    public event Action<IRuntimeSetting>? SettingChanged;

    public RuntimeSetting(ISetting<TValue> setting, bool inputConnected, bool outputConnected) : base(setting)
    {
        Input = new RuntimeConnector<TValue>(Element.Input, inputConnected);
        Output = new RuntimeConnector<TValue>(Element.Output, outputConnected);
    }

    public override void ApplyLoadedValue(object? value, DateTime changedtime)
        => base.ApplyLoadedValue(ValidateValue((TValue?)value), changedtime);

    protected override void OnElementChanged()
    {
        Output.SetValueIfNotEqual(Value);
        SettingChanged?.Invoke(this);
    }

    public virtual void Process(IFunctionBlockCycleInfo cycleInfo)
    {
        if (Input.Changed && Input.Validity == Validity.Valid)
            SetValueIfNotEqual(Input.Value);
    }

    public override bool SetValueIfNotEqual(TValue? value)
        => base.SetValueIfNotEqual(ValidateValue(value));

    protected virtual TValue? ValidateValue(TValue? value)
    {
        foreach (IValidator<TValue> validator in Setting.Design.Validators)
            value = validator.Validate(value, false);
        return value;
    }
}
