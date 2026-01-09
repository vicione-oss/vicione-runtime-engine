using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Execution;

public class InputConnectorInfo(IConnector connector, IExternalIncomingLink incomingLink, IExternalOutgoingLink outgoingLink) : ConnectorInfo(connector)
{
    public IExternalIncomingLink IncomingLink { get; } = incomingLink;
    public IExternalOutgoingLink OutgoingLink { get; } = outgoingLink;
}
