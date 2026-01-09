using System;

namespace ViciOne.Core.Contracts.DataModel;

public interface IPersistentVariable : IPersistable
{
    new DateTime ChangedTime { get; set; }
    IPersistentVariableDesign Design { get; }
    object? Value { get; set; }
}
