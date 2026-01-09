using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class InternalLink<TSource, TValue, TDestination> : InternalLink, IInternalLink<TSource, TValue, TDestination>
{
    public new ConnectorInput<TDestination> Destination
    {
        get => (ConnectorInput<TDestination>)base.Destination;
        set => base.Destination = value;
    }
    IConnector<TDestination> ILinkWithDestination<TValue, TDestination>.Destination => Destination;
    public new ConnectorOutput<TSource> Source
    {
        get => (ConnectorOutput<TSource>)base.Source;
        set => base.Source = value;
    }
    IConnector<TSource> ILinkWithSource<TSource>.Source => Source;

    public InternalLink()
    {
        Destination = new();
        Source = new();
    }
}
