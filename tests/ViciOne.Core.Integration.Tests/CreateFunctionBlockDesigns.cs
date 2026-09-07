using AwesomeAssertions;
using System;
using System.IO;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class CreateFunctionBlockDesigns : LoadingAndSaving
{
    [Theory]
    [InlineData(DesignTimeConstants.BlockDesignAddFileName)]
    [InlineData(DesignTimeConstants.BlockDesignAndFileName)]
    [InlineData(DesignTimeConstants.BlockDesignDriverDemoFileName)]
    [InlineData(DesignTimeConstants.BlockDesignIncrementorFileName)]
    [InlineData(DesignTimeConstants.BlockDesignSettingsDemoFileName)]
    [InlineData(DesignTimeConstants.BlockDesignComplexTypeDemoFileName)]
    public void CanCreateDesign(string fileName)
    {
        var designs = CreateDesigns(PoolingFactory);
        var design = GetDesignByFileName(designs, fileName);

        design.Should().NotBeNull();
    }

    [Theory]
    [InlineData(DesignTimeConstants.BlockDesignAddFileName)]
    [InlineData(DesignTimeConstants.BlockDesignAndFileName)]
    [InlineData(DesignTimeConstants.BlockDesignDriverDemoFileName)]
    [InlineData(DesignTimeConstants.BlockDesignIncrementorFileName)]
    [InlineData(DesignTimeConstants.BlockDesignSettingsDemoFileName)]
    [InlineData(DesignTimeConstants.BlockDesignComplexTypeDemoFileName)]
    public void CanLoadDesign(string fileName)
    {
        var designs = CreateDesigns(PoolingFactory);
        var design = GetDesignByFileName(designs, fileName);
        var workingDir = Directory.CreateTempSubdirectory($"{GetType().Assembly.GetName().Name}_");
        var path = GetDesignPath(workingDir.FullName, fileName);
        Save(design, path);

        var loadedDesign = Load<FunctionBlockDesign>(path);

        loadedDesign.Should().BeEquivalentTo(design);

        workingDir.Delete(true);
    }

    [Theory]
    [InlineData(DesignTimeConstants.BlockDesignAddFileName)]
    [InlineData(DesignTimeConstants.BlockDesignAndFileName)]
    [InlineData(DesignTimeConstants.BlockDesignDriverDemoFileName)]
    [InlineData(DesignTimeConstants.BlockDesignIncrementorFileName)]
    [InlineData(DesignTimeConstants.BlockDesignSettingsDemoFileName)]
    [InlineData(DesignTimeConstants.BlockDesignComplexTypeDemoFileName)]
    public void CanSaveDesign(string fileName)
    {
        var designs = CreateDesigns(PoolingFactory);
        var design = GetDesignByFileName(designs, fileName);
        var path = GetDesignPath(WorkingDir, fileName);

        Save(design, path);

        var fileInfo = new FileInfo(path);
        fileInfo.Exists.Should().BeTrue();
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    internal static FunctionBlockDesigns CreateDesigns(PoolingFactory poolingFactory)
        => new()
        {
            AddDesign = CreateAddDesign(poolingFactory),
            AndDesign = CreateAndDesign(poolingFactory),
            DriverDemoDesign = CreateDriverDemoDesign(poolingFactory),
            IncrementorDesign = CreateIncrementorDesign(poolingFactory),
            SettingsDemoDesign = CreateSettingsDemoDesign(poolingFactory),
            ComplexTypeDemoDesign = CreateComplexTypeDemoDesign(poolingFactory),
        };

    private static FunctionBlockDesign CreateAddDesign(PoolingFactory poolingFactory)
    {
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = Guid.Parse("bc97fbdb-6023-42b9-917a-1dd11c4eb86a");
        designEditor.SetNamespace("Numeric");
        designEditor.SetName("Add");
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs[0]).Id = Guid.Parse("f0aa4c9d-ade5-4c6d-aa8f-ffd5c00e593b");
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs[0]).Id = Guid.Parse("84bc13d4-441a-4366-a9fc-853a2a01ab83");

        designEditor.AddValidRunMode(RunMode.Cyclic);
        designEditor.SetDefaultRunMode(RunMode.Cyclic);

        // would cause an error, because Always is set as the default RunMode
        //designEditor.RemoveValidRunMode(RunMode.Always);

        var connectorEditor1 = new ConnectorDesignInputEditor<double>(DesignTimeConstants.InputDesignAddAddend1Name, poolingFactory);
        connectorEditor1.Design.Id = Guid.Parse("4ac33ded-2b51-46e8-a393-63033ee6144b");
        designEditor.AddProcessDataInput(connectorEditor1);

        var connectorEditor2 = new ConnectorDesignInputEditor<double>(DesignTimeConstants.InputDesignAddAddend2Name, poolingFactory);
        connectorEditor2.Design.Id = Guid.Parse("2ec52c92-1ed2-4dc0-98f6-a420aecbbec0");
        designEditor.AddProcessDataInput(connectorEditor2);

        // would cause an error, because that is a reserved name
        //connectorEditor2.SetName("Enabled");
        // would cause an error, because a connector with this name already exists
        //connectorEditor2.SetName(InputDesignAddAddend1Name);

        var connectorEditor3 = new ConnectorDesignOutputEditor<double>(DesignTimeConstants.OutputDesignAddSumName);
        connectorEditor3.Design.Id = Guid.Parse("2590dc44-a34c-4796-9768-3f992a231f15");
        designEditor.AddProcessDataOutput(connectorEditor3);

        return designEditor.Design;
    }

    private static FunctionBlockDesign CreateAndDesign(PoolingFactory poolingFactory)
    {
        // create the FunctionBlockDesign
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = Guid.Parse("2694b716-af3c-4025-80af-dee0107c7942");
        designEditor.SetNamespace("Logic");
        designEditor.SetName("AND");
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs[0]).Id = Guid.Parse("b984fe66-4318-4646-b627-997a7130a939");
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs[0]).Id = Guid.Parse("ce693f9b-b35f-444e-bad1-a52e049b1c36");

        // create the ConnectorDesigns
        var connectorEditor1 = new ConnectorDesignInputEditor<bool>(DesignTimeConstants.InputDesignAndAName, poolingFactory);
        connectorEditor1.Design.Id = Guid.Parse("4c6edc6b-74a6-4ef7-aedf-d1102a9aa814");
        designEditor.AddProcessDataInput(connectorEditor1);

        var connectorEditor2 = new ConnectorDesignInputEditor<bool>(DesignTimeConstants.InputDesignAndBName, poolingFactory);
        connectorEditor2.Design.Id = Guid.Parse("b6aa1736-42ec-4078-b02f-af65376afd31");
        designEditor.AddProcessDataInput(connectorEditor2);

        var connectorEditor3 = new ConnectorDesignOutputEditor<bool>(DesignTimeConstants.OutputDesignAndYName);
        connectorEditor3.Design.Id = Guid.Parse("76c6a4a8-a8e0-4e6e-b230-b4691a7c1e8c");
        designEditor.AddProcessDataOutput(connectorEditor3);

        return designEditor.Design;
    }

    private static FunctionBlockDesign CreateDriverDemoDesign(PoolingFactory poolingFactory)
    {
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = Guid.Parse("3db9c2b3-d076-4099-85a5-79b2c9418399");
        designEditor.SetNamespace("Demo");
        designEditor.SetName("DriverDemo");
        designEditor.SetRuntimeDriverTypeName("Demo.DemoDriver");
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs[0]).Id = Guid.Parse("8706f9c5-3185-42bd-be7c-ae4d1b20b71d");
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs[0]).Id = Guid.Parse("a989cc30-31f6-40dd-bab6-e6cd988e0b96");

        designEditor.AddValidRunMode(RunMode.Cyclic);
        designEditor.SetDefaultRunMode(RunMode.Cyclic);

        var connectorEditor1 = new ConnectorDesignOutputEditor<DateTime>(DesignTimeConstants.OutputDesignDriverDemoTimeName);
        connectorEditor1.Design.Id = Guid.Parse("c1471b1b-5f6c-4b7e-9a61-59ab6df9e0d9");
        designEditor.AddProcessDataOutput(connectorEditor1);

        var connectorEditor2 = new ConnectorDesignOutputEditor<bool>(DesignTimeConstants.OutputDesignDriverDemoToggleName);
        connectorEditor2.Design.Id = Guid.Parse("76656482-23ee-475d-bf7a-6e5c3a981ffb");
        designEditor.AddProcessDataOutput(connectorEditor2);

        return designEditor.Design;
    }

    private static FunctionBlockDesign CreateIncrementorDesign(PoolingFactory poolingFactory)
    {
        // create the FunctionBlockDesign
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = Guid.Parse("85dbbbd7-1a4e-4b82-94b8-1c34fbaaab60");
        designEditor.SetNamespace("Numeric");
        designEditor.SetName("Incrementor");
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs[0]).Id = Guid.Parse("fff22efe-c07f-4153-98d0-60d83f42271b");
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs[0]).Id = Guid.Parse("5e1db2db-caf0-4a3b-b862-d1c6fb8da391");

        // create the ConnectorDesigns
        var connectorEditor1 = new ConnectorDesignInputEditor<bool>(DesignTimeConstants.InputDesignIncrementorIncrementName, poolingFactory);
        connectorEditor1.Design.Id = Guid.Parse("e14a86af-8d7d-4973-a5f4-e59d80ec2277");
        designEditor.AddProcessDataInput(connectorEditor1);

        var connectorEditor2 = new ConnectorDesignOutputEditor<double>(DesignTimeConstants.OutputDesignIncrementorValueName);
        connectorEditor2.Design.Id = Guid.Parse("b534ddee-d7c0-4ea4-9f2e-24dbfde8851b");
        designEditor.AddProcessDataOutput(connectorEditor2);

        // create the SettingDesign
        var settingEditor = new SettingDesignEditor<double>(DesignTimeConstants.SettingIncrementorIncrementValueName, poolingFactory);
        settingEditor.Setting.Input.Id = Guid.Parse("c7923629-2854-4498-9f21-b3fd67bc4549");
        settingEditor.Setting.Output.Id = Guid.Parse("02f6ebfa-5382-4fa2-b225-ba7aae310fc1");
        settingEditor.SetDefaultValue(0.5);
        settingEditor.AddValidator(new InequalityValidator<double>() { InvalidValue = 0, ReplacementValue = 0.5 });
        designEditor.AddSetting(settingEditor);

        // create the PersistentVariableDesign
        var persistentVariableEditor = new PersistentVariableDesignEditor<double>(DesignTimeConstants.PersistentValueIncrementorValueName);
        persistentVariableEditor.PersistentVariable.Id = Guid.Parse("1cc5f3c2-a5be-4ed7-a934-0ae2ac9e9d50");
        designEditor.AddPersistentVariable(persistentVariableEditor);

        return designEditor.Design;
    }

    private static FunctionBlockDesign CreateSettingsDemoDesign(PoolingFactory poolingFactory)
    {
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = Guid.Parse("510e02ee-b5fc-4625-8fda-37e499f5f1a1");
        designEditor.SetNamespace("Demo");
        designEditor.SetName("SettingsDemo");
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs[0]).Id = Guid.Parse("ad1faf95-b874-4393-9070-d03bb077fb87");
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs[0]).Id = Guid.Parse("4c6e98b4-662d-4fda-bcb5-19a99afffa3c");
        designEditor.AddValidRunMode(RunMode.Cyclic);

        var settingEditor1 = new SettingDesignEditor<bool>(DesignTimeConstants.SettingDummy1Name, poolingFactory);
        settingEditor1.Setting.Input.Id = Guid.Parse("d9bccf18-fadf-4be6-ae02-9c4c9c65edac");
        settingEditor1.Setting.Output.Id = Guid.Parse("c909aa0b-b7b4-4009-b385-fbc37c0bed1b");
        settingEditor1.SetDefaultValue(true);
        designEditor.AddSetting(settingEditor1);

        var settingEditor2 = new SettingDesignEditor<Demo.DummyValues>(DesignTimeConstants.SettingDummy2Name, poolingFactory);
        settingEditor2.Setting.Input.Id = Guid.Parse("5f1bfa66-fb39-437d-bf04-07f27de4e2f5");
        settingEditor2.Setting.Output.Id = Guid.Parse("8cf54142-db9e-4bda-9e49-4b3966aea5b1");
        settingEditor2.AddValidator(new EnumValidator<Demo.DummyValues>() { ReplacementValue = Demo.DummyValues.First });
        settingEditor2.SetDefaultValue(Demo.DummyValues.First);
        designEditor.AddSetting(settingEditor2);

        var connectorEditor1 = new ConnectorDesignInputEditor<double>(DesignTimeConstants.InputDesignSettingsDemoDummyForDefaultPooling1Name, poolingFactory);
        connectorEditor1.Design.Id = Guid.Parse("914c4a77-d907-4543-9ff9-0610c16ef49e");
        connectorEditor1.SetDefaultCrossSourcePoolingStrategy(PoolingStrategy.Queue);
        connectorEditor1.AddValidator(new RangeValidator<double>() { MinValue = -100, MaxValue = 100 });
        designEditor.AddProcessDataInput(connectorEditor1);

        var connectorEditor2 = new ConnectorDesignInputEditor<double[]>(DesignTimeConstants.InputDesignSettingsDemoDummyForDefaultPooling2Name, poolingFactory);
        connectorEditor2.Design.Id = Guid.Parse("c480efa8-c607-4360-b3ff-cd8ce0d7f8ef");
        connectorEditor2.SetDefaultCrossSourcePoolingStrategy(PoolingStrategy.Concat);
        designEditor.AddProcessDataInput(connectorEditor2);

        var connectorEditor3 = new ConnectorDesignInputEditor<bool>(DesignTimeConstants.InputDesignSettingsDemoDummyForDefaultPooling3Name, poolingFactory);
        connectorEditor3.Design.Id = Guid.Parse("b6d86880-58f2-477c-9a87-c4a5f9968c63");
        connectorEditor3.SetDefaultCrossSourcePoolingStrategy(PoolingStrategy.Aggregate, new OrPooling());
        connectorEditor1.SetDefaultPerSourcePoolingStrategy(PoolingStrategy.Queue);
        designEditor.AddProcessDataInput(connectorEditor3);

        return designEditor.Design;
    }

    private static FunctionBlockDesign CreateComplexTypeDemoDesign(PoolingFactory poolingFactory)
    {
        var designEditor = new FunctionBlockDesignEditor(poolingFactory);
        designEditor.Design.Id = Guid.Parse("95403b29-3c37-4c59-9c73-acc6f26358cf");
        designEditor.SetNamespace("Demo");
        designEditor.SetName("ComplexTypeDemo");
        ((ConnectorDesignInput<bool>)designEditor.Design.SystemInputs[0]).Id = Guid.Parse("c0e9efe9-261b-4098-9bcc-7db2b55ed025");
        ((ConnectorDesignOutput<bool>)designEditor.Design.SystemOutputs[0]).Id = Guid.Parse("390958b7-d2eb-41d1-9d1c-91de43c1ba1a");

        var settingEditor1 = new SettingDesignEditor<int>(DesignTimeConstants.SettingComplexTypeDemoNumericValueOffsetName, poolingFactory);
        settingEditor1.Setting.Id = Guid.Parse("935824ad-d082-4140-a4a2-bb254c3d4508");
        settingEditor1.Setting.Input.Id = Guid.Parse("e1d92114-20ed-4340-90c8-37e306c090bf");
        settingEditor1.Setting.Output.Id = Guid.Parse("e5d58cc3-7fe8-4e78-bba8-d454f8b7b290");
        settingEditor1.SetDefaultValue(42);
        designEditor.AddSetting(settingEditor1);

        var connectorEditor1 = new ConnectorDesignInputEditor<Demo.Base>(DesignTimeConstants.InputDesignComplexTypeDemoInName, poolingFactory);
        connectorEditor1.Design.Id = Guid.Parse("eb8b9049-ec2c-407c-849a-3b1065b054cf");
        designEditor.AddProcessDataInput(connectorEditor1);

        var connectorEditor2 = new ConnectorDesignInputEditor<bool>(DesignTimeConstants.InputDesignComplexTypeDemoGenerateBaseName, poolingFactory);
        connectorEditor2.Design.Id = Guid.Parse("2a54a9a4-e558-409f-a5e7-ba00b7eb29d5");
        designEditor.AddProcessDataInput(connectorEditor2);

        var connectorEditor3 = new ConnectorDesignInputEditor<bool>(DesignTimeConstants.InputDesignComplexTypeDemoGenerateDerivedName, poolingFactory);
        connectorEditor3.Design.Id = Guid.Parse("8872c3e4-231c-4ba4-bb29-bec9bfbe361b");
        designEditor.AddProcessDataInput(connectorEditor3);

        var connectorEditor4 = new ConnectorDesignOutputEditor<Demo.Base>(DesignTimeConstants.OutputDesignComplexTypeDemoGeneratedBaseName);
        connectorEditor4.Design.Id = Guid.Parse("92adec66-917a-43ab-ae43-d170f9928e81");
        designEditor.AddProcessDataOutput(connectorEditor4);

        var connectorEditor5 = new ConnectorDesignOutputEditor<Demo.Derived>(DesignTimeConstants.OutputDesignComplexTypeDemoGeneratedDerivedName);
        connectorEditor5.Design.Id = Guid.Parse("0d0e6918-b566-4546-929e-d09c4a32a62c");
        designEditor.AddProcessDataOutput(connectorEditor5);

        var connectorEditor6 = new ConnectorDesignOutputEditor<bool>(DesignTimeConstants.OutputDesignComplexTypeDemoInIsNotNullName);
        connectorEditor6.Design.Id = Guid.Parse("f58206dc-2e2b-4229-974c-31bd420e7a01");
        designEditor.AddProcessDataOutput(connectorEditor6);

        var connectorEditor7 = new ConnectorDesignOutputEditor<int>(DesignTimeConstants.OutputDesignComplexTypeDemoInNumericValueName);
        connectorEditor7.Design.Id = Guid.Parse("036396ce-350c-4242-9709-1af3777d66b6");
        designEditor.AddProcessDataOutput(connectorEditor7);

        var connectorEditor8 = new ConnectorDesignOutputEditor<bool>(DesignTimeConstants.OutputDesignComplexTypeDemoInIsDerivedName);
        connectorEditor8.Design.Id = Guid.Parse("bf0c1827-c442-43f7-9b91-e8df86cb927a");
        designEditor.AddProcessDataOutput(connectorEditor8);

        var connectorEditor9 = new ConnectorDesignOutputEditor<string>(DesignTimeConstants.OutputDesignComplexTypeDemoInStringValueName);
        connectorEditor9.Design.Id = Guid.Parse("d057774b-c11d-4617-92dd-9225deea7cd8");
        designEditor.AddProcessDataOutput(connectorEditor9);

        return designEditor.Design;
    }

    private static string GetDesignPath(string workingDir, string fileName)
        => Path.Combine(workingDir, fileName);

    private static FunctionBlockDesign GetDesignByFileName(FunctionBlockDesigns designs, string fileName)
        => fileName switch
        {
            DesignTimeConstants.BlockDesignAddFileName => designs.AddDesign,
            DesignTimeConstants.BlockDesignAndFileName => designs.AndDesign,
            DesignTimeConstants.BlockDesignDriverDemoFileName => designs.DriverDemoDesign,
            DesignTimeConstants.BlockDesignIncrementorFileName => designs.IncrementorDesign,
            DesignTimeConstants.BlockDesignSettingsDemoFileName => designs.SettingsDemoDesign,
            DesignTimeConstants.BlockDesignComplexTypeDemoFileName => designs.ComplexTypeDemoDesign,
            _ => throw new NotImplementedException(),
        };
}
