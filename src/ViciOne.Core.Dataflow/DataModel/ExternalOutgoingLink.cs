using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Dataflow.DataModel.Stubs;

namespace ViciOne.Core.Dataflow.DataModel;

public abstract class ExternalOutgoingLink : ExternalLink, IExternalOutgoingLink
{
    public Connector Source { get; set; } = EmptyConnector.Default;
    IConnector ILinkWithSource.Source => Source;
}
