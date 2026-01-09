namespace ViciOne.Core.Dataflow.DataModel.Editors;

public interface IConnectorDesignEditor
{
    ConnectorDesign Design { get; }
    FunctionBlockDesignEditor? FunctionBlockDesignEditor { get; set; }

    void SetName(string name);
}
