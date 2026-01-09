using System;

namespace ViciOne.Core.Contracts.DataModel;

public interface IPersistentVariableDesign : IIdProvider
{
    Type PersistentVariableEditorType { get; }
    Type PersistentVariableType { get; }
    object? DefaultValue { get; }
    string Name { get; }
}
