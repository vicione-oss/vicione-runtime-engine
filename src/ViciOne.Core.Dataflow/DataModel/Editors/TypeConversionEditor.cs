using System.ComponentModel;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public class TypeConversionEditor(TypeConversion conversion)
{
    public virtual TypeConversion Conversion { get; } = conversion;

    public TypeConversionEditor(TypeConverter converter) : this(CreateModel(converter)) { }

    private static TypeConversion CreateModel(TypeConverter converter)
        => new() { TypeConverter = converter };

    public virtual void SetTypeConverter(TypeConverter converter)
        => Conversion.TypeConverter = converter;

    public virtual void SetUseConvertFrom(bool useConvertFrom)
        => Conversion.UseConvertFrom = useConvertFrom;
}
