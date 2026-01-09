using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Execution;

public abstract class ConnectorInfo(IConnector connector)
{
    public IConnector Connector { get; } = connector;
}
