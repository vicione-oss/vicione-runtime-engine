using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class InternalLinkEditor<TSource, TValue, TDestination> : TypeConvertableLinkEditor<TSource, TValue>
{
    public InternalLink<TSource, TValue, TDestination> Link { get; }

    public InternalLinkEditor(ConnectorOutput<TSource> source, ConnectorInput<TDestination> destination) : this(source, destination, null) { }

    public InternalLinkEditor(ConnectorOutput<TSource> source, ConnectorInput<TDestination> destination, TypeConversion? typeConversion)
    {
        LinkEditorValidations.ValidateDestinationValueType<TValue, TDestination>();
        Link = CreateModel(source, destination, typeConversion);
    }

    public InternalLinkEditor(InternalLink<TSource, TValue, TDestination> link)
        => Link = link;

    private static InternalLink<TSource, TValue, TDestination> CreateModel(ConnectorOutput<TSource> source, ConnectorInput<TDestination> destination, TypeConversion? typeConversion)
        => new()
        {
            Id = Guid.NewGuid(),
            Source = source,
            Destination = destination,
            TypeConversion = DetermineDefaultTypeConversion(typeConversion)
        };

    public virtual void SetTypeConversion(TypeConversion? typeConversion)
        => Link.TypeConversion = ValidateTypeConversion(typeConversion);
}
