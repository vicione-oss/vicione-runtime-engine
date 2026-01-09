using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.DataModel;

public class ExternalIncomingLink<TSource, TValue, TDestination> : ExternalIncomingLink, IExternalIncomingLink<TSource, TValue, TDestination>
{
    public new ConnectorInput<TDestination> Destination
    {
        get => (ConnectorInput<TDestination>)base.Destination;
        set => base.Destination = value;
    }
    IConnector<TDestination> ILinkWithDestination<TValue, TDestination>.Destination => Destination;
    public new TValue? DefaultValue
    {
        get => (TValue?)base.DefaultValue;
        set => base.DefaultValue = value;
    }

    public ExternalIncomingLink()
    {
        Destination = new();
        DefaultValue = default;
    }
}
