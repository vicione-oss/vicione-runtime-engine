using System.IO;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Filters;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class CreateDataflow2 : CreateDataflow
{
    [Fact]
    public void CreatesDataflow()
    {
        // Dataflow erstellen
        var dataflow = CreateDataflow(
            FBDesigns.AddDesign,
            FBDesigns.DriverDemoDesign,
            FBDesigns.IncrementorDesign);

        // Dataflow als JSON speichern
        Save(dataflow, Path.Combine(WorkingDir, DesignTimeConstants.Dataflow2FileName));
    }

    private static Dataflow CreateDataflow(
        FunctionBlockDesign blockDesignAdd,
        FunctionBlockDesign blockDesignDriverDemo,
        FunctionBlockDesign blockDesignIncrementor)
    {
        // Dataflow erzeugen
        using var dataflowEditor = new DataflowEditor();

        // FB-Designs importieren
        dataflowEditor.AddFunctionBlockDesign(blockDesignAdd);
        dataflowEditor.AddFunctionBlockDesign(blockDesignDriverDemo);
        dataflowEditor.AddFunctionBlockDesign(blockDesignIncrementor);

        // FB-Instanzen erzeugen
        // - Signalgenerator
        var driverDemoFbEditor = new FunctionBlockEditor(blockDesignDriverDemo);
        driverDemoFbEditor.SetName("Generator");
        driverDemoFbEditor.SetRunMode(RunMode.Cyclic);
        driverDemoFbEditor.SetCycleFrequency(50);
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(driverDemoFbEditor);

        // - Incrementor 0,5
        var incrementorFbEditor1 = new FunctionBlockEditor(blockDesignIncrementor);
        incrementorFbEditor1.SetName("Incrementor_0_5");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(incrementorFbEditor1);

        // - Incrementor 2,0
        var incrementorFbEditor2 = new FunctionBlockEditor(blockDesignIncrementor);
        incrementorFbEditor2.SetName("Incrementor_2_0");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(incrementorFbEditor2);

        // - Add
        var addFbEditor = new FunctionBlockEditor(blockDesignAdd);
        addFbEditor.SetName("Add");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(addFbEditor);

        // Einstellungen, für die Standardwerte gesetzt werden sollen, holen
        var incrementor1Setting = (Setting<double>)SettingFilter.GetSettingByName(incrementorFbEditor1.FunctionBlock.Settings, DesignTimeConstants.SettingIncrementorIncrementValueName)!;
        var incrementor2Setting = (Setting<double>)SettingFilter.GetSettingByName(incrementorFbEditor2.FunctionBlock.Settings, DesignTimeConstants.SettingIncrementorIncrementValueName)!;

        // Standardwerte der o.g. Einstellungen setzen
        new SettingEditor<double>(incrementor1Setting).SetValue(0.5);
        new SettingEditor<double>(incrementor2Setting).SetValue(2.0);

        // Eingangs-Konnektoren für Links
        var inputConnectorIncrementor1Increment = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(incrementorFbEditor1.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignIncrementorIncrementName)!;
        var inputConnectorIncrementor2Increment = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(incrementorFbEditor2.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignIncrementorIncrementName)!;
        var inputConnectorAddend1 = (ConnectorInput<double>)ConnectorFilter.GetConnectorByName(addFbEditor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAddAddend1Name)!;
        var inputConnectorAddend2 = (ConnectorInput<double>)ConnectorFilter.GetConnectorByName(addFbEditor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAddAddend2Name)!;

        // Ausgangs-Konnektoren für Links oder Logging
        var outputConnectorToggle = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(driverDemoFbEditor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignDriverDemoToggleName)!;
        var outputConnectorIncrementor1Value = (ConnectorOutput<double>)ConnectorFilter.GetConnectorByName(incrementorFbEditor1.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignIncrementorValueName)!;
        var outputConnectorIncrementor2Value = (ConnectorOutput<double>)ConnectorFilter.GetConnectorByName(incrementorFbEditor2.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignIncrementorValueName)!;
        var outputConnectorAddSum = (ConnectorOutput<double>)ConnectorFilter.GetConnectorByName(addFbEditor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignAddSumName)!;

        // Links erzeugen
        var linkEditor1 = new InternalLinkEditor<bool, bool>(outputConnectorToggle, inputConnectorIncrementor1Increment);
        dataflowEditor.AddInternalLink(linkEditor1.Link);

        var linkEditor2 = new InternalLinkEditor<bool, bool>(outputConnectorToggle, inputConnectorIncrementor2Increment);
        dataflowEditor.AddInternalLink(linkEditor2.Link);

        var linkEditor3 = new InternalLinkEditor<double, double>(outputConnectorIncrementor1Value, inputConnectorAddend1);
        dataflowEditor.AddInternalLink(linkEditor3.Link);

        var linkEditor4 = new InternalLinkEditor<double, double>(outputConnectorIncrementor2Value, inputConnectorAddend2);
        dataflowEditor.AddInternalLink(linkEditor4.Link);

        // bestimmte Konnektoren nach aussen führen (z.B. für Logging)
        var externalLinkEditor1 = new ExternalOutgoingLinkEditor<bool>(outputConnectorToggle);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor1.Link);

        var externalLinkEditor2 = new ExternalOutgoingLinkEditor<double>(outputConnectorIncrementor1Value);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor2.Link);

        var externalLinkEditor3 = new ExternalOutgoingLinkEditor<double>(outputConnectorIncrementor2Value);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor3.Link);

        var externalLinkEditor4 = new ExternalOutgoingLinkEditor<double>(outputConnectorAddSum);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor4.Link);

        return dataflowEditor.Dataflow;
    }
}
