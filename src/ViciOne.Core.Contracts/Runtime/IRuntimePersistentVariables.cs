using System;
using System.Collections.Generic;

namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistentVariables : IEnumerable<IRuntimePersistentVariable>, IDisposable, IChangingElementsProvider<IRuntimePersistentVariable>
{
    event Action<IRuntimePersistentVariable>? VariableChanged;

    void Add(IRuntimePersistentVariable variable);
    void ResetChangedFlags();
}
