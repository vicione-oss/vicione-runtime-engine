namespace ViciOne.Core.Dataflow.DataModel.Editors;

public interface IPersistentVariableDesignEditor
{
    FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }
    PersistentVariableDesign PersistentVariable { get; }
}
