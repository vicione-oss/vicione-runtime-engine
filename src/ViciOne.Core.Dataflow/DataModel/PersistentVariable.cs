using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class PersistentVariable : IPersistentVariable
{
    public DateTime ChangedTime { get; set; }
    public PersistentVariableDesign Design { get; set; } = EmptyPersistentVariableDesign.Default;
    IPersistentVariableDesign IPersistentVariable.Design => Design;
    public Guid Id { get; set; }
    public object? Value { get; set; }
}
