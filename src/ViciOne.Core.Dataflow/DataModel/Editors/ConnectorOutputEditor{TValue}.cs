using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ConnectorOutputEditor<TValue>(ConnectorOutput<TValue> connector) : IConnectorEditor
{
    public ConnectorOutput<TValue> Connector { get; } = connector;
    Connector IConnectorEditor.Connector => Connector;

    public ConnectorOutputEditor(ConnectorDesignOutput<TValue> design) : this(CreateModel(design)) { }

    public virtual void AddValidator(IValidator<TValue> validator)
    {
        ValidateValidatorNotAdded(validator);
        Connector.Validators.Add(validator);
    }

    private static ConnectorOutput<TValue> CreateModel(ConnectorDesignOutput<TValue> design)
    {
        var connector = (ConnectorOutput<TValue>)Activator.CreateInstance(design.ConnectorType)!;
        connector.Design = design;
        connector.Id = Guid.NewGuid();
        connector.MarkAsChangedOnlyIfNotEqual = ConnectorDefaults.MarkAsChangedOnlyIfNotEqual;
        // TODO: does the default value have to be cloned?
        connector.Value = design.DefaultValue;
        return connector;
    }

    public virtual bool RemoveValidator(IValidator<TValue> validator)
        => Connector.Validators.Remove(validator);

    public virtual void ResetToDefaults()
        // TODO: does the default value have to be cloned?
        => SetValue(Connector.Design.DefaultValue);

    public virtual void SetMarkAsChangedOnlyIfNotEqual(bool markAsChangedOnlyIfNotEqual)
        => Connector.MarkAsChangedOnlyIfNotEqual = markAsChangedOnlyIfNotEqual;

    public virtual void SetValue(TValue? value)
        => Connector.Value = ValidateValue(value);

    protected virtual void ValidateValidatorNotAdded(IValidator<TValue> validator)
    {
        if (Connector.Validators.Contains(validator))
            throw new InvalidOperationException("This validator was already added to this connector.");
    }

    protected virtual TValue? ValidateValue(TValue? value)
    {
        // two-stage validation
        foreach (IValidator<TValue> validator in Connector.Design.Validators)
            value = validator.Validate(value, true);
        foreach (IValidator<TValue> validator in Connector.Validators)
            value = validator.Validate(value, true);
        return value;
    }
}
