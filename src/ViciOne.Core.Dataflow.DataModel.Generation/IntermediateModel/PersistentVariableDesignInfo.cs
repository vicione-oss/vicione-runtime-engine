using System;

namespace ViciOne.Core.Dataflow.DataModel.Generation.IntermediateModel;

public sealed class PersistentVariableDesignInfo
{
    public object? DefaultValue { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(object);
}
