using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Execution;

public class OutputConnectorInfo(IConnector connector, IExternalOutgoingLink outgoingLink) : ConnectorInfo(connector)
{
    public IExternalOutgoingLink OutgoingLink { get; } = outgoingLink;
}
