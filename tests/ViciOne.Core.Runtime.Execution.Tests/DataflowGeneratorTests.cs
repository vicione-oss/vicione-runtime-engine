using AwesomeAssertions;
using System;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class DataflowGeneratorTests
{
    public class GenerateDataflow_RelevantAssemblies
    {
        [Fact]
        public void AddsAllDesignsToDataflow()
        {
            var info = DataflowGenerator.GenerateDataflow(new RelevantAssemblies(GetType().Assembly, Enumerable.Empty<Assembly>()));

            info.Dataflow.FunctionBlockDesigns.Select(x => x.Id.ToString()).Should().BeEquivalentTo(
            [
                DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign1.Id,
                DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign2.Id,
                DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign3.Id,
            ]);
        }

        [Fact]
        public void AddsSelectedDesignToDataflow()
        {
            var info = DataflowGenerator.GenerateDataflow(new RelevantAssemblies(GetType().Assembly, []),
                designs => designs.First(x => x.Id.ToString().Equals(DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign2.Id, StringComparison.OrdinalIgnoreCase)));

            info.Dataflow.FunctionBlockDesigns.Select(x => x.Id.ToString()).Should().BeEquivalentTo(
            [
                DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign2.Id,
            ]);
        }

        [Fact]
        public void AddsFilteredDesignsToDataflow()
        {
            var info = DataflowGenerator.GenerateDataflow(new RelevantAssemblies(GetType().Assembly, []),
                designs => designs.Where(
                    x =>
                        x.Id.ToString().Equals(DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign2.Id, StringComparison.OrdinalIgnoreCase) ||
                        x.Id.ToString().Equals(DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign3.Id, StringComparison.OrdinalIgnoreCase)));

            info.Dataflow.FunctionBlockDesigns.Select(x => x.Id.ToString()).Should().BeEquivalentTo(
            [
                DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign2.Id,
                DataflowGeneratorTestsFunctionBlockDesigns.EmptyDesign3.Id,
            ]);
        }
    }

    public class GenerateDataflow_IEnumerable_FunctionBlockDesign
    {
        private FunctionBlockDesign[] Designs { get; } = [CreateFunctionBlockDesign("Design1"), CreateFunctionBlockDesign("Design2")];

        [Fact]
        public void AddsAllDesignsToDataflow()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Dataflow.FunctionBlockDesigns.Should().ContainAllButNotMore(Designs);
        }

        [Fact]
        public void AddsOneBlockForEachDesignToDataflow()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Dataflow.RootContainer.FunctionBlocks.Select(x => x.Design).Should().ContainAllButNotMore(Designs);
        }

        [Fact]
        public void AddsExternalIncomingLinkForEachInput()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Dataflow.Links.OfType<IExternalIncomingLink>().Select(x => x.Destination).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetInputs()));
        }

        [Fact]
        public void AddsExternalOutgoingLinkForEachConnector()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Dataflow.Links.OfType<IExternalOutgoingLink>().Select(x => x.Source).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetConnectors()));
        }

        [Fact]
        public void AddsAllInputsToInputsList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Inputs.Select(x => x.Connector).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetInputs()));
        }

        [Fact]
        public void AddsExternalIncomingLinksToInputsList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Inputs.Select(x => x.IncomingLink.Destination).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetInputs()));
        }

        [Fact]
        public void AddsExternalOutgoingLinksToInputsList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Inputs.Select(x => x.OutgoingLink.Source).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetInputs()));
        }

        [Fact]
        public void AddsAllOutputsToOutputsList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Outputs.Select(x => x.Connector).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetOutputs()));
        }

        [Fact]
        public void AddsExternalOutgoingLinksToOutputsList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Outputs.Select(x => x.OutgoingLink.Source).Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.GetOutputs()));
        }

        [Fact]
        public void AddsAllSettingsToSettingsList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.Settings.Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.Settings));
        }

        [Fact]
        public void AddsAllPersistentVariableToPersistentVariablesList()
        {
            var info = DataflowGenerator.GenerateDataflow(Designs);

            info.PersistentVariables.Should().ContainAllButNotMore(info.Dataflow.RootContainer.FunctionBlocks.SelectMany(x => x.PersistentVariables));
        }

        private static FunctionBlockDesign CreateFunctionBlockDesign(string name)
        {
            PoolingFactory poolingFactory = new([]);
            var editor = new FunctionBlockDesignEditor(poolingFactory);
            editor.SetName(name);
            editor.AddProcessDataInput(new ConnectorDesignInputEditor<bool>("Input1", poolingFactory));
            editor.AddProcessDataInput(new ConnectorDesignInputEditor<int>("Input2", poolingFactory));
            editor.AddProcessDataOutput(new ConnectorDesignOutputEditor<object>("Output1"));
            editor.AddProcessDataOutput(new ConnectorDesignOutputEditor<short>("Output2"));
            editor.AddSetting(new SettingDesignEditor<string>("Setting1", poolingFactory));
            editor.AddSetting(new SettingDesignEditor<double>("Setting2", poolingFactory));
            editor.AddPersistentVariable(new PersistentVariableDesignEditor<long>("Variable1"));
            editor.AddPersistentVariable(new PersistentVariableDesignEditor<float>("Variable2"));
            return editor.Design;
        }
    }
}

namespace DataflowGeneratorTestsFunctionBlockDesigns
{
    [FunctionBlockDesign(Id, "f326cf0d-8b7f-479e-ba07-ffb0a617219d", "128ea036-3ba1-433b-9830-eab29e127c72")]
    public class EmptyDesign1
    {
        internal const string Id = "0b28d155-48be-44e5-8aca-d3125803ad18";
    }

    [FunctionBlockDesign(Id, "720e129a-7f12-49bc-9f2d-fc43d31d0bc4", "342847c2-ae37-468f-b700-b5db4b75fa79")]
    public class EmptyDesign2
    {
        internal const string Id = "868ea690-a4f4-4aab-9626-4dfc1ddd7769";
    }

    [FunctionBlockDesign(Id, "845ba9fb-5373-4b72-8507-2362e15dc73a", "4de2c0d2-8b8f-4d34-9bbf-dae08afeba91")]
    public class EmptyDesign3
    {
        internal const string Id = "8878e764-3d34-4149-ad1a-8c26d767225c";
    }
}
