using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class InternalLink : Link, IInternalLink
{
    public Connector Destination { get; set; } = EmptyConnector.Default;
    IConnector ILinkWithDestination.Destination => Destination;
    public Connector Source { get; set; } = EmptyConnector.Default;
    IConnector ILinkWithSource.Source => Source;
}
