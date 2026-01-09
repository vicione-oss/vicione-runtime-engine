using System;
using ViciOne.Core.Contracts;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

public abstract class TypeConvertableLinkEditor<TSource, TValue>
{
    protected static TypeConversion? DetermineDefaultTypeConversion(TypeConversion? suppliedTypeConversion)
        => TypeConversionResolver.DetermineDefaultTypeConversion(typeof(TSource), typeof(TValue), suppliedTypeConversion, out TypeConversion? typeConversion)
            ? typeConversion
            : throw new ArgumentException($"Conversion from '{typeof(TSource)}' to '{typeof(TValue)}' was not specified and could not be determined automatically.", nameof(suppliedTypeConversion));

    protected virtual TypeConversion? ValidateTypeConversion(TypeConversion? typeConversion)
    {
        DataTypeCompatibilityValidator.ValidateCompatibility<TSource, TValue>(typeConversion);
        return typeConversion;
    }
}
