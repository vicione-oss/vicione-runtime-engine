using System;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;

namespace ViciOne.Core.Dataflow.DataModel;

public class PersistentVariableDesign<TValue> : PersistentVariableDesign, IPersistentVariableDesign<TValue>
{
    public new TValue? DefaultValue
    {
        get => (TValue?)base.DefaultValue;
        set => base.DefaultValue = value;
    }
    public override Type PersistentVariableEditorType => typeof(PersistentVariableEditor<TValue>);
    public override Type PersistentVariableType => typeof(PersistentVariable<TValue>);

    public PersistentVariableDesign()
        => DefaultValue = default;
}
