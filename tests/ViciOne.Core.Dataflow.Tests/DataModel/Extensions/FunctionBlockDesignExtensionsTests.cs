using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Extensions;
using Xunit;

public class FunctionBlockDesignExtensionsTests
{
    public class GetConnectors
    {
        [Fact]
        public void GetsConnectors()
        {
            var info = CreateDesign();

            var connectors = FunctionBlockDesignExtensions.GetConnectors(info.Design);

            // the enumeration has no particular order
            connectors.Should().ContainAllButNotMore(info.Connectors);
        }
    }

    public class GetInputs
    {
        [Fact]
        public void GetsInputs()
        {
            var info = CreateDesign();

            var inputs = FunctionBlockDesignExtensions.GetInputs(info.Design);

            // the enumeration has no particular order
            inputs.Should().ContainAllButNotMore(info.Inputs);
        }
    }

    public class GetOutputs
    {
        [Fact]
        public void GetsOutputs()
        {
            var info = CreateDesign();

            var outputs = FunctionBlockDesignExtensions.GetOutputs(info.Design);

            // the enumeration has no particular order
            outputs.Should().ContainAllButNotMore(info.Outputs);
        }
    }

    private static FunctionBlockDesignInfo CreateDesign()
    {
        var design = new FunctionBlockDesign();

        var setting1 = new SettingDesign<object>();
        var settings = new List<SettingDesign>() { setting1 };
        design.Settings = settings;

        var settingsInput1 = new ConnectorDesignInput<object>();
        var settingsInputs = new List<ConnectorDesign>() { settingsInput1 };
        setting1.Input = settingsInput1;

        var settingsOutput1 = new ConnectorDesignOutput<object>();
        var settingsOutputs = new List<ConnectorDesign>() { settingsOutput1 };
        setting1.Output = settingsOutput1;

        var systemInput1 = new ConnectorDesignInput<string>();
        var systemInput2 = new ConnectorDesignInput<long>();
        var systemInputs = new List<ConnectorDesign>() { systemInput1, systemInput2 };
        design.SystemInputs = systemInputs;

        var processDataInput1 = new ConnectorDesignInput<bool>();
        var processDataInput2 = new ConnectorDesignInput<int>();
        var processDataInputs = new List<ConnectorDesign>() { processDataInput1, processDataInput2 };
        design.ProcessDataInputs = processDataInputs;

        var systemOutput1 = new ConnectorDesignOutput<string>();
        var systemOutputs = new List<ConnectorDesign>() { systemOutput1 };
        design.SystemOutputs = systemOutputs;

        var processDataOutput1 = new ConnectorDesignOutput<bool>();
        var processDataOutputs = new List<ConnectorDesign>() { processDataOutput1 };
        design.ProcessDataOutputs = processDataOutputs;

        var info = new FunctionBlockDesignInfo
        {
            Design = design,
            Inputs = [.. settingsInputs, .. systemInputs, .. processDataInputs],
            Outputs = [.. settingsOutputs, .. systemOutputs, .. processDataOutputs]
        };
        info.Connectors = [.. info.Inputs, .. info.Outputs];

        return info;
    }

    private sealed class FunctionBlockDesignInfo
    {
        public List<ConnectorDesign> Connectors { get; set; } = [];
        public FunctionBlockDesign Design { get; set; } = new();
        public List<ConnectorDesign> Inputs { get; set; } = [];
        public List<ConnectorDesign> Outputs { get; set; } = [];
    }
}
