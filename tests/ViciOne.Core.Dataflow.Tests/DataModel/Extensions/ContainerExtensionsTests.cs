using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Extensions;
using Xunit;

public class ContainerExtensionsTests
{
    public class GetConnectors
    {
        [Fact]
        public void GetsAllConnectors()
        {
            var info = CreateContainer();

            var connectors = ContainerExtensions.GetConnectors(info.Container);

            // the enumeration has no particular order
            connectors.Should().ContainAllButNotMore(info.AllConnectors);
        }
    }

    public class GetContainers
    {
        [Fact]
        public void GetsAllChildContainers()
        {
            var info = CreateContainer();

            var containers = ContainerExtensions.GetContainers(info.Container, true);

            // the enumeration has no particular order
            containers.Should().ContainAllButNotMore(info.AllChildContainers);
        }

        [Fact]
        public void GetsDirectChildContainers()
        {
            var info = CreateContainer();

            var containers = ContainerExtensions.GetContainers(info.Container, false);

            // the enumeration has no particular order
            containers.Should().ContainAllButNotMore(info.DirectChildContainers);
        }
    }

    public class GetFunctionBlocks
    {
        [Fact]
        public void GetsAllChildFunctionBlocks()
        {
            var info = CreateContainer();

            var functionBlocks = ContainerExtensions.GetFunctionBlocks(info.Container, true);

            // the enumeration has no particular order
            functionBlocks.Should().ContainAllButNotMore(info.AllChildFunctionBlocks);
        }

        [Fact]
        public void GetsDirectChildFunctionBlocks()
        {
            var info = CreateContainer();

            var functionBlocks = ContainerExtensions.GetFunctionBlocks(info.Container, false);

            // the enumeration has no particular order
            functionBlocks.Should().ContainAllButNotMore(info.DirectChildFunctionBlocks);
        }
    }

    private static ContainerInfo CreateContainer()
    {
        var container1 = new Container();

        var input1 = new ConnectorInput<bool>();
        var input2 = new ConnectorInput<int>();
        var inputs = new List<Connector>() { input1, input2 };
        container1.Inputs = inputs;

        var output1 = new ConnectorOutput<bool>();
        var outputs = new List<Connector>() { output1 };
        container1.Outputs = outputs;

        var block1 = new FunctionBlock();
        var block2 = new FunctionBlock();
        var blocks = new List<FunctionBlock>() { block1, block2 };
        container1.FunctionBlocks = blocks;

        var childContainer1 = new Container();
        var childContainer2 = new Container();
        var childContainers = new List<Container>() { childContainer1, childContainer2 };
        container1.Containers = childContainers;

        var childContainer2Block1 = new FunctionBlock();
        var childContainer2Block2 = new FunctionBlock();
        var childContainer2Blocks = new List<FunctionBlock>() { childContainer2Block1, childContainer2Block2 };
        childContainer2.FunctionBlocks = childContainer2Blocks;

        var childContainer1ChildContainer1 = new Container();
        var childContainer1ChildContainer2 = new Container();
        var childContainer1ChildContainers = new List<Container>() { childContainer1ChildContainer1, childContainer1ChildContainer2 };
        childContainer1.Containers = childContainer1ChildContainers;

        var childContainer1ChildContainer2Block1 = new FunctionBlock();
        var childContainer1ChildContainer2Block2 = new FunctionBlock();
        var childContainer1ChildContainer2Blocks = new List<FunctionBlock>() { childContainer1ChildContainer2Block1, childContainer1ChildContainer2Block2 };
        childContainer1ChildContainer2.FunctionBlocks = childContainer1ChildContainer2Blocks;

        var info = new ContainerInfo
        {
            Container = container1,
            AllConnectors = [.. inputs, .. outputs],
            DirectChildContainers = childContainers,
            AllChildContainers = [.. childContainers, .. childContainer1ChildContainers],
            DirectChildFunctionBlocks = blocks,
            AllChildFunctionBlocks = [.. blocks, .. childContainer2Blocks, .. childContainer1ChildContainer2Blocks]
        };

        return info;
    }

    private sealed class ContainerInfo
    {
        public List<Container> AllChildContainers { get; set; } = [];
        public List<FunctionBlock> AllChildFunctionBlocks { get; set; } = [];
        public List<Connector> AllConnectors { get; set; } = [];
        public Container Container { get; set; } = new();
        public List<Container> DirectChildContainers { get; set; } = [];
        public List<FunctionBlock> DirectChildFunctionBlocks { get; set; } = [];
    }
}
