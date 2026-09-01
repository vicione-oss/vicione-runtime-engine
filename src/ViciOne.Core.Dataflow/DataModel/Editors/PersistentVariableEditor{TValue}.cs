using System;
using System.Collections.Generic;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class PersistentVariableEditor<TValue>(PersistentVariable<TValue> variable) : IPersistentVariableEditor
{
    public PersistentVariable<TValue> PersistentVariable { get; set; } = variable;
    PersistentVariable IPersistentVariableEditor.PersistentVariable => PersistentVariable;

    public PersistentVariableEditor(PersistentVariableDesign<TValue> design) : this(CreateModel(design)) { }

    private static PersistentVariable<TValue> CreateModel(PersistentVariableDesign<TValue> design)
    {
        var variable = (PersistentVariable<TValue>)Activator.CreateInstance(design.PersistentVariableType)!;
        variable.Design = design;
        variable.Id = Guid.NewGuid();
        // TODO: is it correct that ChangedTime is set to this time?
        variable.ChangedTime = DateTime.UtcNow;
        variable.Value = variable.Design.DefaultValue;
        return variable;
    }

    public virtual void ResetToDefaults()
        => SetValue(PersistentVariable.Design.DefaultValue);

    public virtual void SetValue(TValue? value)
    {
        if (EqualityComparer<TValue?>.Default.Equals(PersistentVariable.Value, value) == false)
            PersistentVariable.ChangedTime = DateTime.UtcNow;

        PersistentVariable.Value = value;
    }
}
