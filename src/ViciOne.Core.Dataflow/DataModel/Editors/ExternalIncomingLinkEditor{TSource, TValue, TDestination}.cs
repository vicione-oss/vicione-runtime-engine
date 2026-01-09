using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class ExternalIncomingLinkEditor<TSource, TValue, TDestination> : TypeConvertableLinkEditor<TSource, TValue>
{
    public ExternalIncomingLink<TSource, TValue, TDestination> Link { get; }

    public ExternalIncomingLinkEditor(ConnectorInput<TDestination> destination) : this(destination, null) { }

    public ExternalIncomingLinkEditor(ConnectorInput<TDestination> destination, TypeConversion? typeConversion)
    {
        LinkEditorValidations.ValidateDestinationValueType<TValue, TDestination>();
        Link = CreateModel(destination, typeConversion);
    }

    public ExternalIncomingLinkEditor(ExternalIncomingLink<TSource, TValue, TDestination> link)
        => Link = link;

    private static ExternalIncomingLink<TSource, TValue, TDestination> CreateModel(ConnectorInput<TDestination> destination, TypeConversion? typeConversion)
        => new()
        {
            Id = Guid.NewGuid(),
            Destination = destination,
            TypeConversion = DetermineDefaultTypeConversion(typeConversion),
        };

    public virtual void SetDefaultValue(TValue? value)
        => Link.DefaultValue = value;

    public virtual void SetTypeConversion(TypeConversion? typeConversion)
        => Link.TypeConversion = ValidateTypeConversion(typeConversion);
}
