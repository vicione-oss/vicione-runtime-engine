namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ExternalIncomingLinkEditor<TSource, TDestination> : ExternalIncomingLinkEditor<TSource, TDestination, TDestination>
{
    public ExternalIncomingLinkEditor(ConnectorInput<TDestination> destination) : base(destination) { }
    public ExternalIncomingLinkEditor(ConnectorInput<TDestination> destination, TypeConversion? typeConversion) : base(destination, typeConversion) { }
    public ExternalIncomingLinkEditor(ExternalIncomingLink<TSource, TDestination, TDestination> link) : base(link) { }
}
