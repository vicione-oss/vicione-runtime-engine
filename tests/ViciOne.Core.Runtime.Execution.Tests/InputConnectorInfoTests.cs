using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class InputConnectorInfoTests
{
    public class Constructor
    {
        [Fact]
        public void StoresIncomingLink()
        {
            var incomingLink = new ExternalIncomingLink<object, object, object>();

            var created = new InputConnectorInfo(new ConnectorInput<object>(), incomingLink, new ExternalOutgoingLink<object>());

            created.IncomingLink.Should().BeSameAs(incomingLink);
        }

        [Fact]
        public void StoresOutgoingLink()
        {
            var outgoingLink = new ExternalOutgoingLink<object>();

            var created = new InputConnectorInfo(new ConnectorInput<object>(), new ExternalIncomingLink<object, object, object>(), outgoingLink);

            created.OutgoingLink.Should().BeSameAs(outgoingLink);
        }
    }
}
