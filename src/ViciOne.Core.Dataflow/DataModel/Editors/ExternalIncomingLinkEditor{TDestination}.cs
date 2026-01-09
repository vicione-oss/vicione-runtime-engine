namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ExternalIncomingLinkEditor<TDestination> : ExternalIncomingLinkEditor<TDestination, TDestination, TDestination>
{
    public ExternalIncomingLinkEditor(ConnectorInput<TDestination> destination) : base(destination) { }
    public ExternalIncomingLinkEditor(ConnectorInput<TDestination> destination, TypeConversion? typeConversion) : base(destination, typeConversion) { }
    public ExternalIncomingLinkEditor(ExternalIncomingLink<TDestination, TDestination, TDestination> link) : base(link) { }
}
