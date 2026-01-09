using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public static class ConnectorFactory
{
    public static TConnector DesignToConnector<TConnector>(ConnectorDesign design) where TConnector : Connector
    {
        var editor = (IConnectorEditor)Activator.CreateInstance(design.ConnectorEditorType, [design])!;
        return (TConnector)editor.Connector;
    }
}
