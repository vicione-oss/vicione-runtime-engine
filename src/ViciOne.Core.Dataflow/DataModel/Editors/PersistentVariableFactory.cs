using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public static class PersistentVariableFactory
{
    public static PersistentVariable DesignToPersistentVariable(PersistentVariableDesign design)
    {
        var editor = (IPersistentVariableEditor)Activator.CreateInstance(design.PersistentVariableEditorType, [design])!;
        return editor.PersistentVariable;
    }
}
