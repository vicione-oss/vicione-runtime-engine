namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class InternalLinkEditor<TSource, TDestination> : InternalLinkEditor<TSource, TDestination, TDestination>
{
    public InternalLinkEditor(ConnectorOutput<TSource> source, ConnectorInput<TDestination> destination) : base(source, destination) { }
    public InternalLinkEditor(ConnectorOutput<TSource> source, ConnectorInput<TDestination> destination, TypeConversion? typeConversion) : base(source, destination, typeConversion) { }
    public InternalLinkEditor(InternalLink<TSource, TDestination, TDestination> link) : base(link) { }
}
