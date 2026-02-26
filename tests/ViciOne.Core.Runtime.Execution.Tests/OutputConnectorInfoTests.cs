using AwesomeAssertions;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Runtime.Execution;
using Xunit;

public class OutputConnectorInfoTests
{
    public class Constructor
    {
        [Fact]
        public void StoresOutgoingLink()
        {
            var outgoingLink = new ExternalOutgoingLink<object>();

            var created = new OutputConnectorInfo(new ConnectorOutput<object>(), outgoingLink);

            created.OutgoingLink.Should().BeSameAs(outgoingLink);
        }
    }
}
