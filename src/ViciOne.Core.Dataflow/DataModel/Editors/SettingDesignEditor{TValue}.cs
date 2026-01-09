using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.Pooling;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class SettingDesignEditor<TValue> : ISettingDesignEditor
{
    private readonly ConnectorDesignInputEditor<TValue> _inputEditor;
    private readonly ConnectorDesignOutputEditor<TValue> _outputEditor;

    public FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }
    public SettingDesign<TValue> Setting { get; }
    SettingDesign ISettingDesignEditor.Setting => Setting;

    public SettingDesignEditor(string name, PoolingFactory poolingFactory)
        => Setting = CreateModel(name, poolingFactory, out _inputEditor, out _outputEditor);

    public SettingDesignEditor(SettingDesign<TValue> setting)
    {
        Setting = setting;
        _inputEditor = new ConnectorDesignInputEditor<TValue>(Setting.Input);
        _outputEditor = new ConnectorDesignOutputEditor<TValue>(Setting.Output);
    }

    public virtual void AddValidator(IValidator<TValue> validator)
    {
        ValidateValidatorNotAdded(validator);
        Setting.Validators.Add(validator);
    }

    private static SettingDesign<TValue> CreateModel(string name, PoolingFactory poolingFactory,
        out ConnectorDesignInputEditor<TValue> inputEditor, out ConnectorDesignOutputEditor<TValue> outputEditor)
    {
        inputEditor = new ConnectorDesignInputEditor<TValue>(name, poolingFactory);
        outputEditor = new ConnectorDesignOutputEditor<TValue>(name);

        var setting = new SettingDesign<TValue>
        {
            Id = Guid.NewGuid(),
            Name = ValidateNameBase(name),
            Input = inputEditor.Design,
            Output = outputEditor.Design
        };

        return setting;
    }

    public virtual bool RemoveValidator(IValidator<TValue> validator)
        => Setting.Validators.Remove(validator);

    public virtual void SetDefaultValue(TValue? value)
    {
        TValue? newValue = ValidateValue(value);
        _inputEditor.SetDefaultValue(newValue);
        _outputEditor.SetDefaultValue(newValue);
        Setting.DefaultValue = newValue;
    }

    public virtual void SetName(string name)
    {
        name = ValidateName(name);
        _inputEditor.SetName(name);
        _outputEditor.SetName(name);
        Setting.Name = name;
    }

    protected virtual string ValidateName(string name)
    {
        name = ValidateNameBase(name);

        if (name.Equals(Setting.Name, StringComparison.Ordinal) == false)
            ValidateNameNotExisting(name);

        return name;
    }

    protected static string ValidateNameBase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"{nameof(name)} cannot be null or empty.", nameof(name));

        name = name.Trim();
        if (NameValidator.IsValidNamespaceOrClassName(name) == false)
            throw ExceptionFactory.CreateNoValidNamespaceOrClassNameException(name);

        return name;
    }

    protected virtual void ValidateNameNotExisting(string name)
        => FunctionBlockDesignEditor?.ValidateSettingDesignNameNotExisting(name);

    protected virtual void ValidateValidatorNotAdded(IValidator<TValue> validator)
    {
        if (Setting.Validators.Contains(validator))
            throw new InvalidOperationException("This validator was already added to this design.");
    }

    protected virtual TValue? ValidateValue(TValue? value)
    {
        foreach (IValidator<TValue> validator in Setting.Validators)
            value = validator.Validate(value, true);
        return value;
    }
}
