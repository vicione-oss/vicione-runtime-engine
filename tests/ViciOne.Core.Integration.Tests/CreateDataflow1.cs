using System;
using System.IO;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Filters;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class CreateDataflow1 : CreateDataflow
{
    [Fact]
    public void CreatesDataflow()
    {
        // Dataflow erstellen
        var dataflow = CreateDataflow(
            FBDesigns.AddDesign,
            FBDesigns.AndDesign,
            FBDesigns.DriverDemoDesign,
            FBDesigns.IncrementorDesign,
            FBDesigns.SettingsDemoDesign,
            FBDesigns.ComplexTypeDemoDesign);

        // Dataflow als JSON speichern
        Save(dataflow, Path.Combine(WorkingDir, DesignTimeConstants.Dataflow1FileName));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1506:Übermäßige Klassenkopplungen vermeiden", Justification = "Umgestaltung für Tests nicht notwendig")]
    private static Dataflow CreateDataflow(
        FunctionBlockDesign blockDesignAdd,
        FunctionBlockDesign blockDesignAnd,
        FunctionBlockDesign blockDesignDriverDemo,
        FunctionBlockDesign blockDesignIncrementor,
        FunctionBlockDesign blockDesignSettingsDemo,
        FunctionBlockDesign blockDesignComplexTypeDemo)
    {
        // Dataflow erzeugen
        using var dataflowEditor = new DataflowEditor();

        // FB Designs importieren
        dataflowEditor.AddFunctionBlockDesign(blockDesignAdd);
        dataflowEditor.AddFunctionBlockDesign(blockDesignAnd);
        dataflowEditor.AddFunctionBlockDesign(blockDesignDriverDemo);
        dataflowEditor.AddFunctionBlockDesign(blockDesignIncrementor);
        dataflowEditor.AddFunctionBlockDesign(blockDesignSettingsDemo);
        dataflowEditor.AddFunctionBlockDesign(blockDesignComplexTypeDemo);

        // würde Fehler erzeugen, da schon ein FB-Design mit diesem Namen vorhanden ist
        //dataflowEditor.AddFunctionBlockDesign(CreateAddDesign());

        // ein paar Container erzeugen
        var containerEditor1 = new ContainerEditor();
        containerEditor1.SetName("Explizit festgelegter Name");
        dataflowEditor.RootContainerEditor.AddContainer(containerEditor1);

        var containerEditor2 = new ContainerEditor();
        containerEditor1.AddContainer(containerEditor2);

        // Instanzen erzeugen
        // - Source 0
        var sourceFb0Editor = new FunctionBlockEditor(blockDesignAdd);
        sourceFb0Editor.SetName("Source 0");
        // würde Fehler erzeugen, da der RunMode nicht gültig ist
        //sourceFb0Editor.SetRunMode(RunMode.Always);
        sourceFb0Editor.SetCycleFrequency(1);
        sourceFb0Editor.SetRunMode(RunMode.Cyclic);
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(sourceFb0Editor);

        // - Source 1
        var sourceFb1Editor = new FunctionBlockEditor(blockDesignAnd);
        sourceFb1Editor.SetName("Source 1");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(sourceFb1Editor);

        // - Source 2
        var sourceFb2Editor = new FunctionBlockEditor(blockDesignAnd);
        sourceFb2Editor.SetName("Source 2");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(sourceFb2Editor);

        // - Target
        var targetFbEditor = new FunctionBlockEditor(blockDesignAnd);
        targetFbEditor.SetName("Target");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(targetFbEditor);

        // - Treiber Demo
        var driverDemoFbEditor = new FunctionBlockEditor(blockDesignDriverDemo);
        driverDemoFbEditor.SetRunMode(RunMode.Cyclic);
        driverDemoFbEditor.SetCycleFrequency(50);
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(driverDemoFbEditor);

        // - Settings Demo 1
        var settingsDemoFbEditor1 = new FunctionBlockEditor(blockDesignSettingsDemo);
        settingsDemoFbEditor1.SetName("Settings Demo 1");
        settingsDemoFbEditor1.SetRunMode(RunMode.Cyclic);
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(settingsDemoFbEditor1);

        // - Settings Demo 2
        var settingsDemoFbEditor2 = new FunctionBlockEditor(blockDesignSettingsDemo);
        settingsDemoFbEditor2.SetName("Settings Demo 2");
        settingsDemoFbEditor2.SetRunMode(RunMode.Cyclic);
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(settingsDemoFbEditor2);

        // - Incrementor
        var incrementorFbEditor = new FunctionBlockEditor(blockDesignIncrementor);
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(incrementorFbEditor);

        // - Complex Type Demo 1
        var complexTypeDemoFbEditor1 = new FunctionBlockEditor(blockDesignComplexTypeDemo);
        complexTypeDemoFbEditor1.SetName("Complex Type Demo 1");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(complexTypeDemoFbEditor1);

        // - Complex Type Demo 2
        var complexTypeDemoFbEditor2 = new FunctionBlockEditor(blockDesignComplexTypeDemo);
        complexTypeDemoFbEditor2.SetName("Complex Type Demo 2");
        // dem Container hinzufügen
        dataflowEditor.RootContainerEditor.AddFunctionBlock(complexTypeDemoFbEditor2);

        // hiermit können alle FunktionsBlöcke rekursiv abgefragt werden
        // IEnumerable<FunctionBlock> allBlocks = dataflowEditor.Dataflow.RootContainer.GetFunctionBlocks(true);

        // hiermit können alle Container eines Dataflows abgefragt werden
        // IEnumerable<Container> allContainers = dataflowEditor.Dataflow.GetContainers();

        // hiermit kann der Container eines FunktionBlockes abgefagt werden
        // Container containerBlock1 = dataflowEditor.Dataflow.GetContainer(fbEditor.FunctionBlock);

        // Einstellungen, für die Standardwerte gesetzt werden sollen, holen
        var dummySetting1 = (Setting<bool>)SettingFilter.GetSettingByName(settingsDemoFbEditor1.FunctionBlock.Settings, DesignTimeConstants.SettingDummy1Name)!;
        var dummySetting2 = (Setting<Demo.DummyValues>)SettingFilter.GetSettingByName(settingsDemoFbEditor1.FunctionBlock.Settings, DesignTimeConstants.SettingDummy2Name)!;
        var complexTypeDemoFbEditor2OffsetSetting = (Setting<int>)SettingFilter.GetSettingByName(complexTypeDemoFbEditor2.FunctionBlock.Settings, DesignTimeConstants.SettingComplexTypeDemoNumericValueOffsetName)!;

        // Eingangs-Konnektoren, für die Standardwerte gesetzt werden sollen, holen
        var inputConnector0Addend1 = (ConnectorInput<double>)ConnectorFilter.GetConnectorByName(sourceFb0Editor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAddAddend1Name)!;
        var inputConnector0Addend2 = (ConnectorInput<double>)ConnectorFilter.GetConnectorByName(sourceFb0Editor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAddAddend2Name)!;
        var inputConnector1A = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(sourceFb1Editor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAndAName)!;
        var inputConnector1B = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(sourceFb1Editor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAndBName)!;
        var inputConnector2A = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(sourceFb2Editor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAndAName)!;
        var inputConnector2B = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(sourceFb2Editor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAndBName)!;
        var inputConnector3B = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(targetFbEditor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAndBName)!;
        var inputConnectorDummyForPooling1_1 = (ConnectorInput<double>)ConnectorFilter.GetConnectorByName(settingsDemoFbEditor1.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignSettingsDemoDummyForDefaultPooling1Name)!;
        var inputConnectorDummyForPooling1_2 = (ConnectorInput<double[]>)ConnectorFilter.GetConnectorByName(settingsDemoFbEditor1.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignSettingsDemoDummyForDefaultPooling2Name)!;

        // Standardwerte der o.g. Einstellungen setzen
        new SettingEditor<bool>(dummySetting1).SetValue(true);
        new SettingEditor<Demo.DummyValues>(dummySetting2).SetValue(Demo.DummyValues.Second);
        new SettingEditor<int>(complexTypeDemoFbEditor2OffsetSetting).SetValue(52);
        // Folgender Aufruf würde zu einer Exception führen, da der Wertebereich des Validators überschritten wird (Wert nicht im Enum definiert)
        //new SettingEditor<Demo.DummyEnum>(dummySetting2).SetValue((Demo.DummyEnum)100);
        // HACK: sicherstellen, dass der persistierte Wert benutzt wird, falls vorhanden
        dummySetting1.ChangedTime = DateTime.MinValue.ToUniversalTime();
        dummySetting2.ChangedTime = DateTime.MinValue.ToUniversalTime();
        complexTypeDemoFbEditor2OffsetSetting.ChangedTime = DateTime.MinValue.ToUniversalTime();

        // Standardwerte der o.g. Inputs setzen
        var addend1Editor = new ConnectorInputEditor<double>(inputConnector0Addend1);
        addend1Editor.SetValue(2.5);
        new ConnectorInputEditor<double>(inputConnector0Addend2).SetValue(0.6);
        new ConnectorInputEditor<bool>(inputConnector1A).SetValue(true);
        new ConnectorInputEditor<bool>(inputConnector1B).SetValue(true);
        new ConnectorInputEditor<bool>(inputConnector2A).SetValue(true);
        new ConnectorInputEditor<bool>(inputConnector2B).SetValue(false);
        new ConnectorInputEditor<bool>(inputConnector3B).SetValue(true);

        // MarkAsChangedOnlyIfNotEqual setzen
        addend1Editor.SetMarkAsChangedOnlyIfNotEqual(false);

        // Konnektor-Instanz-Validatoren hinterlegen
        var inputEditorInputConnectorDummyForPooling1_1 = new ConnectorInputEditor<double>(inputConnectorDummyForPooling1_1);
        inputEditorInputConnectorDummyForPooling1_1.AddValidator(new RangeValidator<double>() { MinValue = 50, MaxValue = 75 });
        inputEditorInputConnectorDummyForPooling1_1.AddValidator(new InequalityValidator<double>() { InvalidValue = 66 });
        inputEditorInputConnectorDummyForPooling1_1.SetValue(55);
        // Folgender Aufruf würde zu einer Exception führen, da der Wertebereich des Validators überschritten wird
        //inputEditorInputConnectorDummyForPooling1_1.SetValue(80);
        // Folgender Aufruf würde zu einer Exception führen, da der zweite Validator diesen Wert als ungültig markiert
        //inputEditorInputConnectorDummyForPooling1_1.SetValue(66);
        // Dieser Validator hätte einen größeren Wertebereich als der, der schon am Design hinterlegt ist. Daher wird er zwar benutzt, hat aber keine Auswirkungen
        //inputEditorInputConnectorDummyForPooling1_1.SetValidator(new RangeValidator<double>() { MinValue = -200, MaxValue = 200 });

        // Ausgangs-Konnektoren für Links oder Logging
        var outputConnectorAddSum = (ConnectorOutput<double>)ConnectorFilter.GetConnectorByName(sourceFb0Editor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignAddSumName)!;
        var outputConnectorY1 = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(sourceFb1Editor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignAndYName)!;
        var outputConnectorY2 = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(sourceFb2Editor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignAndYName)!;
        var outputConnectorY3 = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(targetFbEditor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignAndYName)!;
        var outputConnectorToggle = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(driverDemoFbEditor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignDriverDemoToggleName)!;
        var outputConnectorIncrementValue = (ConnectorOutput<double>)ConnectorFilter.GetConnectorByName(incrementorFbEditor.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignIncrementorValueName)!;
        var outputConnectorDummySetting = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(settingsDemoFbEditor1.FunctionBlock.SettingsOutputs, DesignTimeConstants.SettingDummy1Name)!;
        var outputConnectorComplexGeneratedDerived1 = (ConnectorOutput<Demo.Derived>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignComplexTypeDemoGeneratedDerivedName)!;
        var outputConnectorComplexGeneratedDerived2 = (ConnectorOutput<Demo.Derived>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor2.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignComplexTypeDemoGeneratedDerivedName)!;
        var outputConnectorComplexInIsNotNull = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignComplexTypeDemoInIsNotNullName)!;
        var outputConnectorComplexInNumericValue = (ConnectorOutput<int>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignComplexTypeDemoInNumericValueName)!;
        var outputConnectorComplexInIsDerived = (ConnectorOutput<bool>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignComplexTypeDemoInIsDerivedName)!;
        var outputConnectorComplexInStringValue = (ConnectorOutput<string>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataOutputs, DesignTimeConstants.OutputDesignComplexTypeDemoInStringValueName)!;

        // Ziel-Eingangs-Konnektoren
        var inputConnector3A = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(targetFbEditor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignAndAName)!;
        inputConnector3A.CrossSourcePoolingStrategy = PoolingStrategy.Aggregate;
        // wahlweise eine der folgenden Pooling-Implementierungen
        //inputConnector3A.Pooling = new AndPooling();
        inputConnector3A.CrossSourceAggregatingPooling = new OrPooling();
        //inputConnector3A.Pooling = new MaxPooling();
        //inputConnector3A.Pooling = new MinPooling();
        var inputConnectorIncrement = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(incrementorFbEditor.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignIncrementorIncrementName)!;
        var inputConnectorComplexIn = (ConnectorInput<Demo.Base>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignComplexTypeDemoInName)!;
        var inputConnectorComplexGenerateDerived1 = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor1.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignComplexTypeDemoGenerateDerivedName)!;
        var inputConnectorComplexGenerateDerived2 = (ConnectorInput<bool>)ConnectorFilter.GetConnectorByName(complexTypeDemoFbEditor2.FunctionBlock.ProcessDataInputs, DesignTimeConstants.InputDesignComplexTypeDemoGenerateDerivedName)!;

        // Link von double-Quelle-1 zu bool-Ziel-1
        var linkEditor1 = new InternalLinkEditor<double, bool>(outputConnectorAddSum, inputConnector3A);
        dataflowEditor.AddInternalLink(linkEditor1.Link);
        // Link von bool-Quelle-1 zu bool-Ziel-1
        var linkEditor2 = new InternalLinkEditor<bool, bool>(outputConnectorY1, inputConnector3A);
        dataflowEditor.AddInternalLink(linkEditor2.Link);
        // Link von bool-Quelle-2 zu bool-Ziel-1
        var linkEditor3 = new InternalLinkEditor<bool, bool>(outputConnectorY2, inputConnector3A);
        dataflowEditor.AddInternalLink(linkEditor3.Link);
        // Link von Demo-Toggle zu Incrementor-Increment
        var linkEditor4 = new InternalLinkEditor<bool, bool>(outputConnectorToggle, inputConnectorIncrement);
        dataflowEditor.AddInternalLink(linkEditor4.Link);
        // Link von Complex-Demo-GeneratedDerived 1 zu Complex-Demo-In (1)
        var linkEditor5 = new InternalLinkEditor<Demo.Derived, Demo.Base>(outputConnectorComplexGeneratedDerived1, inputConnectorComplexIn);
        dataflowEditor.AddInternalLink(linkEditor5.Link);
        // Link von Complex-Demo-GeneratedDerived 2 zu Complex-Demo-In (1)
        var linkEditor5_2 = new InternalLinkEditor<Demo.Derived, Demo.Base>(outputConnectorComplexGeneratedDerived2, inputConnectorComplexIn);
        dataflowEditor.AddInternalLink(linkEditor5_2.Link);
        // Link von Demo-Toggle zu Complex-Demo-GenerateDerived 1
        var linkEditor6 = new InternalLinkEditor<bool, bool>(outputConnectorToggle, inputConnectorComplexGenerateDerived1);
        dataflowEditor.AddInternalLink(linkEditor6.Link);
        // Link von Demo-Toggle zu Complex-Demo-GenerateDerived 2
        var linkEditor6_2 = new InternalLinkEditor<bool, bool>(outputConnectorToggle, inputConnectorComplexGenerateDerived2);
        dataflowEditor.AddInternalLink(linkEditor6_2.Link);

        // würde Fehler erzeugen, weil schon ein Link mit dieser Quelle und diesem Ziel existiert
        //var linkEditor11 = new LinkEditor<double, bool>(sourceConnectorAddSum, targetConnector);
        //dataflowEditor.AddLink(linkEditor11.Link);

        // Link von extern zu einem double-Eingangskonnektor
        var externalLinkEditor1 = new ExternalIncomingLinkEditor<double>(inputConnector0Addend1);
        dataflowEditor.AddExternalIncomingLink(externalLinkEditor1.Link);

        // Weiterer Link von extern zum gleichen double-Eingangskonnektor
        var externalLinkEditor1_1 = new ExternalIncomingLinkEditor<double>(inputConnector0Addend1);
        dataflowEditor.AddExternalIncomingLink(externalLinkEditor1_1.Link);

        // Link von extern zu einem double-Eingangskonnektor mit Typ-Konvertierung
        var externalLinkEditor1_2 = new ExternalIncomingLinkEditor<int, double>(inputConnector0Addend1);
        dataflowEditor.AddExternalIncomingLink(externalLinkEditor1_2.Link);

        // Link von einem double-Ausgangskonnektor nach extern
        var externalLinkEditor2 = new ExternalOutgoingLinkEditor<double>(outputConnectorAddSum);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor2.Link);

        // Weiterer Link von gleichem double-Ausgangskonnektor nach extern
        var externalLinkEditor2_1 = new ExternalOutgoingLinkEditor<double>(outputConnectorAddSum);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor2_1.Link);

        // Link von extern zu einem double-Eingangskonnektor mit verkettendem Pooling
        var externalLinkEditor3 = new ExternalIncomingLinkEditor<double, double, double[]>(inputConnectorDummyForPooling1_2);
        dataflowEditor.AddExternalIncomingLink(externalLinkEditor3.Link);

        // Weiterer Link von extern zu gleichen double-Eingangskonnektor mit verkettendem Pooling
        var externalLinkEditor4 = new ExternalIncomingLinkEditor<double, double, double[]>(inputConnectorDummyForPooling1_2);
        dataflowEditor.AddExternalIncomingLink(externalLinkEditor4.Link);

        // Link von einem weiteren double-Ausgangskonnektor nach extern
        var externalLinkEditor5 = new ExternalOutgoingLinkEditor<double>(outputConnectorIncrementValue);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor5.Link);

        // Link von einem weiteren bool-Ausgangskonnektor nach extern
        var externalLinkEditor7 = new ExternalOutgoingLinkEditor<bool>(outputConnectorToggle);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor7.Link);

        // Link von einem weiteren bool-Ausgangskonnektor nach extern
        var externalLinkEditor8 = new ExternalOutgoingLinkEditor<bool>(outputConnectorY3);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor8.Link);

        // Link von einem Einstellungs-bool-Ausgangskonnektor nach extern
        var externalLinkEditor9 = new ExternalOutgoingLinkEditor<bool>(outputConnectorDummySetting);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor9.Link);

        var externalLinkEditor10 = new ExternalOutgoingLinkEditor<bool>(outputConnectorComplexInIsNotNull);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor10.Link);

        var externalLinkEditor11 = new ExternalOutgoingLinkEditor<int>(outputConnectorComplexInNumericValue);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor11.Link);

        var externalLinkEditor12 = new ExternalOutgoingLinkEditor<bool>(outputConnectorComplexInIsDerived);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor12.Link);

        var externalLinkEditor13 = new ExternalOutgoingLinkEditor<string>(outputConnectorComplexInStringValue);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor13.Link);

        // einen Eingangskonnektor ausgehend nach aussen führen (z.B. für Logging)
        var externalLinkEditor6 = new ExternalOutgoingLinkEditor<bool>(inputConnectorIncrement);
        dataflowEditor.AddExternalOutgoingLink(externalLinkEditor6.Link);

        return dataflowEditor.Dataflow;
    }
}
