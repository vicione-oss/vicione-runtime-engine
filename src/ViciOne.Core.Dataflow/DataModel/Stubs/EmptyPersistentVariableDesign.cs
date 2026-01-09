using System;

namespace ViciOne.Core.Dataflow.DataModel.Stubs;

internal sealed class EmptyPersistentVariableDesign : PersistentVariableDesign
{
    public static EmptyPersistentVariableDesign Default { get; } = new();

    public override Type PersistentVariableEditorType => typeof(object);
    public override Type PersistentVariableType => typeof(object);
}
