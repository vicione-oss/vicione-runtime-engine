using AwesomeAssertions;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class ConnectorInfoTests
{
    public class Constructor
    {
        [Fact]
        public void StoresConnector()
        {
            var connector = new ConnectorInput<object>();

            var created = new MockConnectorInfo(connector);

            created.Connector.Should().BeSameAs(connector);
        }

        private class MockConnectorInfo(IConnector connector) : ConnectorInfo(connector) { }
    }
}
