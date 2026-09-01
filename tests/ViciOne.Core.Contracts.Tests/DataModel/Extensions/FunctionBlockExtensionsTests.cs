using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using Xunit;

public class FunctionBlockExtensionsTests
{
    public class GetConnectors
    {
        [Fact]
        public void GetsConnectors()
        {
            var info = CreateBlock();

            var connectors = FunctionBlockExtensions.GetConnectors(info.Block);

            // the enumeration has no particular order
            connectors.Should().ContainAllButNotMore(info.Connectors);
        }
    }

    public class GetInputs
    {
        [Fact]
        public void GetsInputs()
        {
            var info = CreateBlock();

            var inputs = FunctionBlockExtensions.GetInputs(info.Block);

            // the enumeration has no particular order
            inputs.Should().ContainAllButNotMore(info.Inputs);
        }
    }

    public class GetOutputs
    {
        [Fact]
        public void GetsOutputs()
        {
            var info = CreateBlock();

            var outputs = FunctionBlockExtensions.GetOutputs(info.Block);

            // the enumeration has no particular order
            outputs.Should().ContainAllButNotMore(info.Outputs);
        }
    }

    private static FunctionBlockInfo CreateBlock()
    {
        var block = Substitute.For<IFunctionBlock>();

        var setting1 = Substitute.For<ISetting>();
        var settings = new List<ISetting>() { setting1 };
        block.Settings.Returns(settings);

        var settingsInput1 = Substitute.For<IConnector<object>>();
        var settingsInputs = new List<IConnector>() { settingsInput1 };
        setting1.Input.Returns(settingsInput1);
        block.SettingsInputs.Returns(settingsInputs);

        var settingsOutput1 = Substitute.For<IConnector<object>>();
        var settingsOutputs = new List<IConnector>() { settingsOutput1 };
        setting1.Output.Returns(settingsOutput1);
        block.SettingsOutputs.Returns(settingsOutputs);

        var systemInput1 = Substitute.For<IConnector<string>>();
        var systemInput2 = Substitute.For<IConnector<long>>();
        var systemInputs = new List<IConnector>() { systemInput1, systemInput2 };
        block.SystemInputs.Returns(systemInputs);

        var processDataInput1 = Substitute.For<IConnector<bool>>();
        var processDataInput2 = Substitute.For<IConnector<int>>();
        var processDataInputs = new List<IConnector>() { processDataInput1, processDataInput2 };
        block.ProcessDataInputs.Returns(processDataInputs);

        var systemOutput1 = Substitute.For<IConnector<string>>();
        var systemOutputs = new List<IConnector>() { systemOutput1 };
        block.SystemOutputs.Returns(systemOutputs);

        var processDataOutput1 = Substitute.For<IConnector<bool>>();
        var processDataOutputs = new List<IConnector>() { processDataOutput1 };
        block.ProcessDataOutputs.Returns(processDataOutputs);

        var info = new FunctionBlockInfo
        {
            Block = block,
            Inputs = [.. settingsInputs, .. systemInputs, .. processDataInputs],
            Outputs = [.. settingsOutputs, .. systemOutputs, .. processDataOutputs]
        };
        info.Connectors = [.. info.Inputs, .. info.Outputs];

        return info;
    }

    private sealed class FunctionBlockInfo
    {
        public IFunctionBlock Block { get; set; } = Substitute.For<IFunctionBlock>();
        public List<IConnector> Connectors { get; set; } = [];
        public List<IConnector> Inputs { get; set; } = [];
        public List<IConnector> Outputs { get; set; } = [];
    }
}
