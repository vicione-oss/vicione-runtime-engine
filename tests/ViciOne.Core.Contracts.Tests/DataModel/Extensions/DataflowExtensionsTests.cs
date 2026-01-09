using AwesomeAssertions;
using NSubstitute;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Extensions;
using Xunit;

public class DataflowExtensionsTests
{
    public class GetContainer
    {
        public static TheoryData<IDataflow, IFunctionBlock, IContainer?> ArgumentsFor_GetsCorrectContainer
        {
            get
            {
                var info = CreateDataflow();
                TheoryData<IDataflow, IFunctionBlock, IContainer?> theoryData = [];
                foreach (var pair in info.FunctionBlocksContainer)
                    theoryData.Add(info.Dataflow, pair.Key, pair.Value);
                theoryData.Add(info.Dataflow, Substitute.For<IFunctionBlock>(), null);
                return theoryData;
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsCorrectContainer))]
        public void GetsCorrectContainer(IDataflow dataflow, IFunctionBlock functionBlock, IContainer? expectedContainer)
        {
            var container = DataflowExtensions.GetContainer(dataflow, functionBlock);

            container.Should().BeSameAs(expectedContainer);
        }
    }

    public class GetContainers
    {
        [Fact]
        public void GetsAllContainers()
        {
            var info = CreateDataflow();

            var containers = DataflowExtensions.GetContainers(info.Dataflow);

            // die Auflistung hat keine bestimmte Reihenfolge
            containers.Should().ContainAllButNotMore(info.AllContainers);
        }
    }

    private static DataflowInfo CreateDataflow()
    {
        var dataflow = Substitute.For<IDataflow>();

        var rootContainer = Substitute.For<IContainer>();
        dataflow.RootContainer.Returns(rootContainer);

        var block1 = Substitute.For<IFunctionBlock>();
        var block2 = Substitute.For<IFunctionBlock>();
        var blocks = new List<IFunctionBlock>() { block1, block2 };
        rootContainer.FunctionBlocks.Returns(blocks);

        var childContainer1 = Substitute.For<IContainer>();
        var childContainer2 = Substitute.For<IContainer>();
        var childContainers = new List<IContainer>() { childContainer1, childContainer2 };
        rootContainer.Containers.Returns(childContainers);

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

        var info = new DataflowInfo
        {
            Dataflow = dataflow,
            AllContainers = [.. childContainers, .. childContainer1ChildContainers]
        };
        info.AllContainers.Add(rootContainer);
        info.AllChildFunctionBlocks = [.. blocks, .. childContainer2Blocks, .. childContainer1ChildContainer2Blocks];
        info.FunctionBlocksContainer = new Dictionary<IFunctionBlock, IContainer>()
        {
            { block1, rootContainer},
            { block2, rootContainer},
            { childContainer2Block1, childContainer2},
            { childContainer2Block2, childContainer2},
            { childContainer1ChildContainer2Block1, childContainer1ChildContainer2},
            { childContainer1ChildContainer2Block2, childContainer1ChildContainer2},
        };

        return info;
    }

    private sealed class DataflowInfo
    {
        public List<IContainer> AllContainers { get; set; } = [];
        public List<IFunctionBlock> AllChildFunctionBlocks { get; set; } = [];
        public IDataflow Dataflow { get; set; } = Substitute.For<IDataflow>();
        public Dictionary<IFunctionBlock, IContainer> FunctionBlocksContainer { get; set; } = [];
    }
}
