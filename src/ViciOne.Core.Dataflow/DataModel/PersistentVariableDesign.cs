using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class PersistentVariableDesign : IPersistentVariableDesign
{
    public object? DefaultValue { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public abstract Type PersistentVariableEditorType { get; }
    public abstract Type PersistentVariableType { get; }
}
