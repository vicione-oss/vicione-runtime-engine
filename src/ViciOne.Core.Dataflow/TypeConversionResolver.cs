using System;
using System.ComponentModel;
using ViciOne.Core.Contracts;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.DefaultTypeConverters;

namespace ViciOne.Core.Dataflow;

public static class TypeConversionResolver
{
    static TypeConversionResolver()
        => RegisterDefaultTypeConverters();

    public static bool DetermineDefaultTypeConversion(Type sourceValueType, Type destinationValueType,
        TypeConversion? suppliedTypeConversion, out TypeConversion? typeConversion)
    {
        if (sourceValueType == destinationValueType ||
            destinationValueType.IsAssignableFrom(sourceValueType))
        {
            typeConversion = null;
            return true;
        }

        if (suppliedTypeConversion is not null && DataTypeCompatibilityValidator.CanConvert(suppliedTypeConversion, sourceValueType, destinationValueType))
        {
            typeConversion = suppliedTypeConversion;
            return true;
        }

        TypeConverter sourceConverter = TypeDescriptor.GetConverter(sourceValueType);
        if (sourceConverter.CanConvertTo(destinationValueType))
        {
            typeConversion = new TypeConversion { TypeConverter = sourceConverter, UseConvertFrom = false };
            return true;
        }

        TypeConverter destinationConverter = TypeDescriptor.GetConverter(destinationValueType);
        if (destinationConverter.CanConvertFrom(sourceValueType))
        {
            typeConversion = new TypeConversion { TypeConverter = destinationConverter, UseConvertFrom = true };
            return true;
        }

        typeConversion = null;
        return false;
    }

    private static void RegisterDefaultTypeConverters()
        => TypeDescriptor.AddAttributes(typeof(DateTime), new TypeConverterAttribute(typeof(DateAndTimeToDateTimeConverter)));
}
