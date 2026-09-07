using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
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
        var container1 = Substitute.For<IContainer>();

        var input1 = Substitute.For<IConnector<bool>>();
        var input2 = Substitute.For<IConnector<int>>();
        var inputs = new List<IConnector>() { input1, input2 };
        container1.Inputs.Returns(inputs);

        var output1 = Substitute.For<IConnector<bool>>();
        var outputs = new List<IConnector>() { output1 };
        container1.Outputs.Returns(outputs);

        var block1 = Substitute.For<IFunctionBlock>();
        var block2 = Substitute.For<IFunctionBlock>();
        var blocks = new List<IFunctionBlock>() { block1, block2 };
        container1.FunctionBlocks.Returns(blocks);

        var childContainer1 = Substitute.For<IContainer>();
        var childContainer2 = Substitute.For<IContainer>();
        var childContainers = new List<IContainer>() { childContainer1, childContainer2 };
        container1.Containers.Returns(childContainers);

        var childContainer2Block1 = Substitute.For<IFunctionBlock>();
        var childContainer2Block2 = Substitute.For<IFunctionBlock>();
        var childContainer2Blocks = new List<IFunctionBlock>() { childContainer2Block1, childContainer2Block2 };
        childContainer2.FunctionBlocks.Returns(childContainer2Blocks);

        var childContainer1ChildContainer1 = Substitute.For<IContainer>();
        var childContainer1ChildContainer2 = Substitute.For<IContainer>();
        var childContainer1ChildContainers = new List<IContainer>() { childContainer1ChildContainer1, childContainer1ChildContainer2 };
        childContainer1.Containers.Returns(childContainer1ChildContainers);

        var childContainer1ChildContainer2Block1 = Substitute.For<IFunctionBlock>();
        var childContainer1ChildContainer2Block2 = Substitute.For<IFunctionBlock>();
        var childContainer1ChildContainer2Blocks = new List<IFunctionBlock>() { childContainer1ChildContainer2Block1, childContainer1ChildContainer2Block2 };
        childContainer1ChildContainer2.FunctionBlocks.Returns(childContainer1ChildContainer2Blocks);

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
        public List<IContainer> AllChildContainers { get; set; } = [];
        public List<IFunctionBlock> AllChildFunctionBlocks { get; set; } = [];
        public List<IConnector> AllConnectors { get; set; } = [];
        public IContainer Container { get; set; } = Substitute.For<IContainer>();
        public List<IContainer> DirectChildContainers { get; set; } = [];
        public List<IFunctionBlock> DirectChildFunctionBlocks { get; set; } = [];
    }
}
