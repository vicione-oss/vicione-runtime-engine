using AwesomeAssertions;
using System.Collections.Generic;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Extensions;
using Xunit;

public class DataflowExtensionsTests
{
    public class GetContainer
    {
        public static TheoryData<Dataflow, FunctionBlock, Container?> ArgumentsFor_GetsCorrectContainer
        {
            get
            {
                var info = CreateDataflow();
                TheoryData<Dataflow, FunctionBlock, Container?> theoryData = [];
                foreach (var pair in info.FunctionBlocksContainer)
                    theoryData.Add(info.Dataflow, pair.Key, pair.Value);
                theoryData.Add(info.Dataflow, new FunctionBlock(), null);
                return theoryData;
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentsFor_GetsCorrectContainer))]
        public void GetsCorrectContainer(Dataflow dataflow, FunctionBlock functionBlock, Container? expectedContainer)
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

            // the enumeration has no particular order
            containers.Should().ContainAllButNotMore(info.AllContainers);
        }
    }

    private static DataflowInfo CreateDataflow()
    {
        var dataflow = new Dataflow();

        var rootContainer = new Container();
        dataflow.RootContainer = rootContainer;

        var block1 = new FunctionBlock();
        var block2 = new FunctionBlock();
        var blocks = new List<FunctionBlock>() { block1, block2 };
        rootContainer.FunctionBlocks = blocks;

        var childContainer1 = new Container();
        var childContainer2 = new Container();
        var childContainers = new List<Container>() { childContainer1, childContainer2 };
        rootContainer.Containers = childContainers;

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

        var info = new DataflowInfo
        {
            Dataflow = dataflow,
            AllContainers = [.. childContainers, .. childContainer1ChildContainers]
        };
        info.AllContainers.Add(rootContainer);
        info.AllChildFunctionBlocks = [.. blocks, .. childContainer2Blocks, .. childContainer1ChildContainer2Blocks];
        info.FunctionBlocksContainer = new Dictionary<FunctionBlock, Container>()
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
        public List<Container> AllContainers { get; set; } = [];
        public List<FunctionBlock> AllChildFunctionBlocks { get; set; } = [];
        public Dataflow Dataflow { get; set; } = new();
        public Dictionary<FunctionBlock, Container> FunctionBlocksContainer { get; set; } = [];
    }
}
