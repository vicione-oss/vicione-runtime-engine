using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Editors;
using Xunit;

public class ConnectorFactoryTests
{
    public class DesignToConnector
    {
        [Fact]
        public void CreatesConnector()
        {
            var design = new ConnectorDesignInput<object>();

            var connector = ConnectorFactory.DesignToConnector<Connector>(design);

            connector.Should().NotBeNull();
        }
    }
}
