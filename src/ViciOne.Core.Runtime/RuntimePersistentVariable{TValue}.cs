using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Runtime;

public class RuntimePersistentVariable<TValue>(IPersistentVariable<TValue> variable) :
    RuntimePersistableElement<IPersistentVariable<TValue>, TValue>(variable), IRuntimePersistentVariable<TValue>
{
    public IPersistentVariable<TValue> Variable => Element;
    IPersistentVariable IRuntimePersistentVariable.Variable => Element;

    public event Action<IRuntimePersistentVariable>? VariableChanged;

    protected override void OnElementChanged()
        => VariableChanged?.Invoke(this);
}
