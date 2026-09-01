using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
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

    public class GetRuntimeTypeName
    {
        [Fact]
        public void GetsCorrectNameIfExplicitNameWasSet()
        {
            var expectedRuntimeTypeName = $"Zweiungvierzig";
            var design = CreateDesign(expectedRuntimeTypeName).Design;

            var returned = FunctionBlockDesignExtensions.GetRuntimeTypeName(design);

            returned.Should().Be(expectedRuntimeTypeName);
        }

        [Fact]
        public void GetsCorrectNameIfExplicitNameWasNotSet()
        {
            var design = CreateDesign().Design;
            var expectedRuntimeTypeName = $"{design.Namespace}.{design.Name}";

            var returned = FunctionBlockDesignExtensions.GetRuntimeTypeName(design);

            returned.Should().Be(expectedRuntimeTypeName);
        }
    }

    private static FunctionBlockDesignInfo CreateDesign(string? runtimeTypeName = null)
    {
        var design = Substitute.For<IFunctionBlockDesign>();
        design.Namespace.Returns("Namespace");
        design.Name.Returns("Name");
        design.RuntimeTypeName.Returns(runtimeTypeName);

        var setting1 = Substitute.For<ISettingDesign>();
        var settings = new List<ISettingDesign>() { setting1 };
        design.Settings.Returns(settings);

        var settingsInput1 = Substitute.For<IConnectorDesign<object>>();
        var settingsInputs = new List<IConnectorDesign>() { settingsInput1 };
        setting1.Input.Returns(settingsInput1);

        var settingsOutput1 = Substitute.For<IConnectorDesign<object>>();
        var settingsOutputs = new List<IConnectorDesign>() { settingsOutput1 };
        setting1.Output.Returns(settingsOutput1);

        var systemInput1 = Substitute.For<IConnectorDesign<string>>();
        var systemInput2 = Substitute.For<IConnectorDesign<long>>();
        var systemInputs = new List<IConnectorDesign>() { systemInput1, systemInput2 };
        design.SystemInputs.Returns(systemInputs);

        var processDataInput1 = Substitute.For<IConnectorDesign<bool>>();
        var processDataInput2 = Substitute.For<IConnectorDesign<int>>();
        var processDataInputs = new List<IConnectorDesign>() { processDataInput1, processDataInput2 };
        design.ProcessDataInputs.Returns(processDataInputs);

        var systemOutput1 = Substitute.For<IConnectorDesign<string>>();
        var systemOutputs = new List<IConnectorDesign>() { systemOutput1 };
        design.SystemOutputs.Returns(systemOutputs);

        var processDataOutput1 = Substitute.For<IConnectorDesign<bool>>();
        var processDataOutputs = new List<IConnectorDesign>() { processDataOutput1 };
        design.ProcessDataOutputs.Returns(processDataOutputs);

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
        public List<IConnectorDesign> Connectors { get; set; } = [];
        public IFunctionBlockDesign Design { get; set; } = Substitute.For<IFunctionBlockDesign>();
        public List<IConnectorDesign> Inputs { get; set; } = [];
        public List<IConnectorDesign> Outputs { get; set; } = [];
    }
}
