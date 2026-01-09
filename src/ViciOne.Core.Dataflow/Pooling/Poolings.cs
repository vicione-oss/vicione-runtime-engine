using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.DataModel.Attributes;

namespace ViciOne.Core.Dataflow.Pooling;

internal static class Poolings
{
    internal static IEnumerable<Type> GetAvailablePoolingTypes<T>(IEnumerable<Assembly> assemblies)
        => TypeFilter.GetAvailableInterfaceImplementingTypes(typeof(IAggregatingPooling<>), typeof(IAggregatingPooling<T>), assemblies);

    internal static IEnumerable<Type> GetAvailablePoolingTypes(IEnumerable<Assembly> assemblies, Type valueType)
        => TypeFilter.GetAvailableInterfaceImplementingTypes(typeof(IAggregatingPooling<>), typeof(IAggregatingPooling<>).MakeGenericType(valueType), assemblies);

    internal static Type? GetDefaultPoolingType<T>(IEnumerable<Type> availablePoolingTypes)
        => GetDefaultPoolingType(availablePoolingTypes, typeof(T));

    internal static Type? GetDefaultPoolingType(IEnumerable<Type> availablePoolingTypes, Type valueType)
    {
        foreach (Type availableType in availablePoolingTypes)
            if (availableType.GetCustomAttributes<DefaultPoolingForTypeAttribute>()
                    .Any(x => x.Type == valueType))
                return availableType;

        return null;
    }
}
