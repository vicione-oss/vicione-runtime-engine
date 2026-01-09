using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class PersistentVariable<TValue> : PersistentVariable, IPersistentVariable<TValue>
{
    public new PersistentVariableDesign<TValue> Design
    {
        get => (PersistentVariableDesign<TValue>)base.Design;
        set => base.Design = value;
    }
    IPersistentVariableDesign<TValue> IPersistentVariable<TValue>.Design => Design;
    public new TValue? Value
    {
        get => (TValue?)base.Value;
        set => base.Value = value;
    }

    public PersistentVariable()
    {
        Design = new PersistentVariableDesign<TValue>();
        Value = default;
    }
}
