using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class ExternalIncomingLinkFactoryTests
{
    public class CreateAndAddToDataflow
    {
        [Fact]
        public void CreatesLink()
        {
            using var dataflowEditor = new DataflowEditor();
            var destinationConnector = CreateAndAddInput<int>(dataflowEditor.Dataflow);
            using var factory = new ExternalIncomingLinkFactory();

            var link = factory.CreateAndAddToDataflow(destinationConnector, dataflowEditor);

            link.Should().BeOfType<ExternalIncomingLink<int, int, int>>();
            link.Destination.Should().BeSameAs(destinationConnector);
        }

        [Fact]
        public void AddsLinkToDataflow()
        {
            using var dataflowEditor = new DataflowEditor();
            var destinationConnector = CreateAndAddInput<int>(dataflowEditor.Dataflow);
            using var factory = new ExternalIncomingLinkFactory();

            var link = factory.CreateAndAddToDataflow(destinationConnector, dataflowEditor);

            dataflowEditor.Dataflow.Links.Should().Contain(link);
        }

        private static ConnectorInput<T> CreateAndAddInput<T>(Dataflow dataflow)
        {
            var block = new FunctionBlock();
            var connector = new ConnectorInput<T>();
            block.ProcessDataInputs.Add(connector);
            dataflow.RootContainer.FunctionBlocks.Add(block);
            return connector;
        }
    }
}
