using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class ExternalIncomingLink : ExternalLink, IExternalIncomingLink
{
    public object? DefaultValue { get; set; }
    public Connector Destination { get; set; } = EmptyConnector.Default;
    IConnector ILinkWithDestination.Destination => Destination;
}
