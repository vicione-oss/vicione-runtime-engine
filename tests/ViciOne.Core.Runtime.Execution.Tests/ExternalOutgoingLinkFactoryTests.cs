using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class ExternalOutgoingLinkFactoryTests
{
    public class CreateAndAddToDataflow
    {
        [Fact]
        public void CreatesLinkFromInput()
        {
            using var dataflowEditor = new DataflowEditor();
            var sourceConnector = CreateAndAddInput<int>(dataflowEditor.Dataflow);
            using var factory = new ExternalOutgoingLinkFactory();

            var link = factory.CreateAndAddToDataflow(sourceConnector, dataflowEditor);

            link.Should().BeOfType<ExternalOutgoingLink<int>>();
            link.Source.Should().BeSameAs(sourceConnector);
        }

        [Fact]
        public void CreatesLinkFromOutput()
        {
            using var dataflowEditor = new DataflowEditor();
            var sourceConnector = CreateAndAddOutput<int>(dataflowEditor.Dataflow);
            using var factory = new ExternalOutgoingLinkFactory();

            var link = factory.CreateAndAddToDataflow(sourceConnector, dataflowEditor);

            link.Should().BeOfType<ExternalOutgoingLink<int>>();
            link.Source.Should().BeSameAs(sourceConnector);
        }

        [Fact]
        public void AddsLinkToDataflow()
        {
            using var dataflowEditor = new DataflowEditor();
            var sourceConnector = CreateAndAddInput<int>(dataflowEditor.Dataflow);
            using var factory = new ExternalOutgoingLinkFactory();

            var link = factory.CreateAndAddToDataflow(sourceConnector, dataflowEditor);

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

        private static ConnectorOutput<T> CreateAndAddOutput<T>(Dataflow dataflow)
        {
            var block = new FunctionBlock();
            var connector = new ConnectorOutput<T>();
            block.ProcessDataOutputs.Add(connector);
            dataflow.RootContainer.FunctionBlocks.Add(block);
            return connector;
        }
    }
}
