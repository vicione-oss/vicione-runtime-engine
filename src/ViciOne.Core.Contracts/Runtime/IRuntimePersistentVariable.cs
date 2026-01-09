using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistentVariable : IRuntimePersistableElement
{
    IPersistentVariable Variable { get; }

    event Action<IRuntimePersistentVariable>? VariableChanged;
}
