using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Contracts.Runtime.Extensions;
using Xunit;

public class RuntimeFunctionBlockExtensionsTests
{
    public class GetConnectors
    {
        [Fact]
        public void GetsConnectors()
        {
            var info = CreateBlock();

            var connectors = RuntimeFunctionBlockExtensions.GetConnectors(info.Block);

            // die Auflistung hat keine bestimmte Reihenfolge
            connectors.Should().ContainAllButNotMore(info.Connectors);
        }
    }

    public class GetInputs
    {
        [Fact]
        public void GetsInputs()
        {
            var info = CreateBlock();

            var inputs = RuntimeFunctionBlockExtensions.GetInputs(info.Block);

            // die Auflistung hat keine bestimmte Reihenfolge
            inputs.Should().ContainAllButNotMore(info.Inputs);
        }
    }

    public class GetOutputs
    {
        [Fact]
        public void GetsOutputs()
        {
            var info = CreateBlock();

            var outputs = RuntimeFunctionBlockExtensions.GetOutputs(info.Block);

            // die Auflistung hat keine bestimmte Reihenfolge
            outputs.Should().ContainAllButNotMore(info.Outputs);
        }
    }

    private static RuntimeFunctionBlockInfo CreateBlock()
    {
        var block = Substitute.For<IRuntimeFunctionBlock>();

        var setting1 = Substitute.For<IRuntimeSetting>();
        var settings = Substitute.For<IRuntimeSettings>();
        var settingsList = new List<IRuntimeSetting>() { setting1 };
        settings.GetEnumerator().Returns(settingsList.GetEnumerator());
        block.Settings.Returns(settings);

        var settingsInput1 = Substitute.For<IRuntimeConnector<object>>();
        var settingsInputs = new List<IRuntimeConnector>() { settingsInput1 };
        setting1.Input.Returns(settingsInput1);
        block.SettingsInputs.Returns(settingsInputs);

        var settingsOutput1 = Substitute.For<IRuntimeConnector<object>>();
        var settingsOutputs = new List<IRuntimeConnector>() { settingsOutput1 };
        setting1.Output.Returns(settingsOutput1);
        block.SettingsOutputs.Returns(settingsOutputs);

        var systemInput1 = Substitute.For<IRuntimeConnector<string>>();
        var systemInput2 = Substitute.For<IRuntimeConnector<long>>();
        var systemInputs = new List<IRuntimeConnector>() { systemInput1, systemInput2 };
        block.SystemInputs.Returns(systemInputs);

        var processDataInput1 = Substitute.For<IRuntimeConnector<bool>>();
        var processDataInput2 = Substitute.For<IRuntimeConnector<int>>();
        var processDataInputs = new List<IRuntimeConnector>() { processDataInput1, processDataInput2 };
        block.ProcessDataInputs.Returns(processDataInputs);

        var systemOutput1 = Substitute.For<IRuntimeConnector<string>>();
        var systemOutputs = new List<IRuntimeConnector>() { systemOutput1 };
        block.SystemOutputs.Returns(systemOutputs);

        var processDataOutput1 = Substitute.For<IRuntimeConnector<bool>>();
        var processDataOutputs = new List<IRuntimeConnector>() { processDataOutput1 };
        block.ProcessDataOutputs.Returns(processDataOutputs);

        var info = new RuntimeFunctionBlockInfo
        {
            Block = block,
            Inputs = [.. settingsInputs, .. systemInputs, .. processDataInputs],
            Outputs = [.. settingsOutputs, .. systemOutputs, .. processDataOutputs]
        };
        info.Connectors = [.. info.Inputs, .. info.Outputs];

        return info;
    }

    private sealed class RuntimeFunctionBlockInfo
    {
        public IRuntimeFunctionBlock Block { get; set; } = Substitute.For<IRuntimeFunctionBlock>();
        public List<IRuntimeConnector> Connectors { get; set; } = [];
        public List<IRuntimeConnector> Inputs { get; set; } = [];
        public List<IRuntimeConnector> Outputs { get; set; } = [];
    }
}
