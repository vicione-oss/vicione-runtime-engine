using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ConnectorDesignOutputEditor<TValue>(ConnectorDesignOutput<TValue> design) : IConnectorDesignEditor
{
    public ConnectorDesignOutput<TValue> Design { get; } = design;
    ConnectorDesign IConnectorDesignEditor.Design => Design;
    public FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }

    public ConnectorDesignOutputEditor(string name) : this(CreateModel(name)) { }

    public virtual void AddValidator(IValidator<TValue> validator)
    {
        ValidateValidatorNotAdded(validator);
        Design.Validators.Add(validator);
    }

    private static ConnectorDesignOutput<TValue> CreateModel(string name)
    {
        var design = new ConnectorDesignOutput<TValue>
        {
            Id = Guid.NewGuid(),
            Name = ValidateNameBase(name),
        };
        return design;
    }

    protected virtual bool IsSystemReservedName(string name)
        => IsSystemReservedNameBase(name);

    protected static bool IsSystemReservedNameBase(string name)
        => name.Equals(SystemConnectorNames.FunctionBlockEnabledName, StringComparison.OrdinalIgnoreCase);

    public virtual bool RemoveValidator(IValidator<TValue> validator)
        => Design.Validators.Remove(validator);

    public virtual void SetDefaultValue(TValue? value)
        => Design.DefaultValue = ValidateValue(value);

    public virtual void SetName(string name)
        => Design.Name = ValidateName(name);

    protected virtual string ValidateName(string name)
    {
        name = ValidateNameBase(name);

        if (name.Equals(Design.Name, StringComparison.Ordinal) == false)
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

        if (IsSystemReservedNameBase(name))
            throw new ArgumentException("The specified name is a reserved system connector name.", nameof(name));

        return name;
    }

    protected virtual void ValidateNameNotExisting(string name)
        => FunctionBlockDesignEditor?.ValidateConnectorDesignNameNotExisting(Design, name);

    protected virtual void ValidateValidatorNotAdded(IValidator<TValue> validator)
    {
        if (Design.Validators.Contains(validator))
            throw new InvalidOperationException("This validator was already added to this design.");
    }

    protected virtual TValue? ValidateValue(TValue? value)
    {
        foreach (IValidator<TValue> validator in Design.Validators)
            value = validator.Validate(value, true);
        return value;
    }
}
