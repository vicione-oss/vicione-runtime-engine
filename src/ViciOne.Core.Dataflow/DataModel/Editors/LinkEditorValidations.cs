using System;

namespace ViciOne.Core.Dataflow.DataModel.Editors;

internal static class LinkEditorValidations
{
    public static void ValidateDestinationValueType<TValue, TDestination>()
        => ValidateDestinationValueType(typeof(TValue), typeof(TDestination));

    public static void ValidateDestinationValueType(Type valueType, Type destinationType)
    {
        if (destinationType == valueType)
            return;
        if (destinationType.IsArray &&
            destinationType.GetElementType() == valueType)
            return;
        throw new InvalidOperationException("Destination type must either be the same as value type or an array of value type.");
    }
}
