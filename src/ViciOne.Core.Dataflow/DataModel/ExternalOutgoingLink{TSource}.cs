using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class ExternalOutgoingLink<TSource> : ExternalOutgoingLink, IExternalOutgoingLink<TSource>
{
    public new IConnector<TSource> Source
    {
        get => (IConnector<TSource>)base.Source;
        set => base.Source = (Connector)value;
    }
    IConnector<TSource> ILinkWithSource<TSource>.Source => Source;

    public ExternalOutgoingLink()
        => Source = new ConnectorOutput<TSource>();
}
