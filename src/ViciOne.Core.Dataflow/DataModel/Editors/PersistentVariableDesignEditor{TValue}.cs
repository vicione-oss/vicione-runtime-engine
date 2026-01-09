using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class PersistentVariableDesignEditor<TValue>(PersistentVariableDesign<TValue> persistentVariable) : IPersistentVariableDesignEditor
{
    public FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }
    public PersistentVariableDesign<TValue> PersistentVariable { get; } = persistentVariable;
    PersistentVariableDesign IPersistentVariableDesignEditor.PersistentVariable => PersistentVariable;

    public PersistentVariableDesignEditor(string name) : this(CreateModel(name)) { }

    private static PersistentVariableDesign<TValue> CreateModel(string name)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = ValidateNameBase(name)
        };

    public virtual void SetDefaultValue(TValue? value)
        => PersistentVariable.DefaultValue = value;

    public virtual void SetName(string name)
        => PersistentVariable.Name = ValidateName(name);

    protected virtual string ValidateName(string name)
    {
        name = ValidateNameBase(name);

        if (name.Equals(PersistentVariable.Name, StringComparison.Ordinal) == false)
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
        => FunctionBlockDesignEditor?.ValidatePersistentVariableDesignNameNotExisting(name);
}
