using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Extensions;
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

            connectors.Should().BeEquivalentTo(info.Connectors);
        }
    }

    public class GetInputs
    {
        [Fact]
        public void GetsInputs()
        {
            var info = CreateBlock();

            var inputs = FunctionBlockExtensions.GetInputs(info.Block);

            inputs.Should().BeEquivalentTo(info.Inputs);
        }
    }

    public class GetOutputs
    {
        [Fact]
        public void GetsOutputs()
        {
            var info = CreateBlock();

            var outputs = FunctionBlockExtensions.GetOutputs(info.Block);

            outputs.Should().BeEquivalentTo(info.Outputs);
        }
    }

    private static FunctionBlockInfo CreateBlock()
    {
        var block = new FunctionBlock();

        var setting1 = new Setting<object>();
        var settings = new List<Setting>() { setting1 };
        block.Settings = settings;

        var settingsInput1 = new ConnectorInput<object>();
        var settingsInputs = new List<Connector>() { settingsInput1 };
        setting1.Input = settingsInput1;

        var settingsOutput1 = new ConnectorOutput<object>();
        var settingsOutputs = new List<Connector>() { settingsOutput1 };
        setting1.Output = settingsOutput1;

        var systemInput1 = new ConnectorInput<string>();
        var systemInput2 = new ConnectorInput<long>();
        var systemInputs = new List<Connector>() { systemInput1, systemInput2 };
        block.SystemInputs = systemInputs;

        var processDataInput1 = new ConnectorInput<bool>();
        var processDataInput2 = new ConnectorInput<int>();
        var processDataInputs = new List<Connector>() { processDataInput1, processDataInput2 };
        block.ProcessDataInputs = processDataInputs;

        var systemOutput1 = new ConnectorOutput<string>();
        var systemOutputs = new List<Connector>() { systemOutput1 };
        block.SystemOutputs = systemOutputs;

        var processDataOutput1 = new ConnectorOutput<bool>();
        var processDataOutputs = new List<Connector>() { processDataOutput1 };
        block.ProcessDataOutputs = processDataOutputs;

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
        public FunctionBlock Block { get; set; } = new();
        public List<Connector> Connectors { get; set; } = [];
        public List<Connector> Inputs { get; set; } = [];
        public List<Connector> Outputs { get; set; } = [];
    }
}
