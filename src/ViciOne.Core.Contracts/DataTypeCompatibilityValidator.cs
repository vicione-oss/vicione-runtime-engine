using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Core.Contracts;

public static class DataTypeCompatibilityValidator
{
    private static readonly Dictionary<Type, Func<Type, Type>> s_knownGenericInterfaceTypeDefinitions = new()
    {
        { typeof(IConnector<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
        { typeof(ILink<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
        { typeof(IPersistable<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
        { typeof(IRuntimeConnector<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
        { typeof(IRuntimeLink<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
        { typeof(IAggregatingPooling<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
        { typeof(IValidator<>), (interfaceType) => interfaceType.GetGenericArguments()[0] },
    };

    public static bool CanConvert(ITypeConversion typeConversion, Type sourceValueType, Type destinationValueType)
        => typeConversion.UseConvertFrom
            ? typeConversion.TypeConverter.CanConvertFrom(sourceValueType)
            : typeConversion.TypeConverter.CanConvertTo(destinationValueType);

    public static Type DetermineExternalIncomingLinkSourceType(IExternalIncomingLink link)
    {
        IEnumerable<Type> candidateImplementedInterfaceTypes = GetCandidateImplementedInterfaceTypes(link.GetType().GetTypeInfo());
        foreach (Type implementedInterfaceType in candidateImplementedInterfaceTypes)
        {
            Type genericTypeDefinition = implementedInterfaceType.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(IExternalIncomingLink<,,>))
                return implementedInterfaceType.GetGenericArguments()[0];
        }
        throw new ArgumentException($"Source value type could not be determined. The specified link does not implement the correct {nameof(IExternalIncomingLink)} interface.", nameof(link));
    }

    public static Type DetermineLinkValueType(IConnector connector)
    {
        Type connectorValueType = DetermineValueType(connector);
        Type linkValueType;
        if (connector is IPoolable poolableConnector && poolableConnector.CrossSourcePoolingStrategy == PoolingStrategy.Concat)
        {
            if (connectorValueType.IsArray == false)
                throw new InvalidOperationException($"Invalid pooling configuration. '{nameof(PoolingStrategy.Concat)}' pooling strategy could only be used for array connectors.");
            linkValueType = connectorValueType.GetElementType()!;
        }
        else
            linkValueType = connectorValueType;
        return linkValueType;
    }

    public static Type DetermineLinkValueType(IRuntimeConnector connector)
        => DetermineLinkValueType(connector.Connector);

    public static Type DetermineValueType(object obj)
        => DetermineValueType(obj.GetType());

    public static Type DetermineValueType(Type type)
    {
        Type[] valueTypes = DetermineValueTypes(type);
        return valueTypes.Length > 1
            ? throw new ArgumentException($"The specified type provides multiple value types. Use {nameof(DetermineValueTypes)} instead.", nameof(type))
            : valueTypes.FirstOrDefault() ?? throw new ArgumentException($"Value type could not be determined.", nameof(type));
    }

    public static Type[] DetermineValueTypes(object obj)
        => DetermineValueTypes(obj.GetType());

    public static Type[] DetermineValueTypes(Type type)
    {
        Type[] valueTypes = [.. GetValueTypesIfTypeImplementsKnownInterface(type.GetTypeInfo())];
        return valueTypes.Length > 0 || type.BaseType == null
            ? valueTypes
            : DetermineValueTypes(type.BaseType);
    }

    private static IEnumerable<Type> GetCandidateImplementedInterfaceTypes(TypeInfo typeInfo)
    {
        if (typeInfo.IsInterface && typeInfo.IsGenericType && typeInfo.IsGenericTypeDefinition == false)
            yield return typeInfo;
        foreach (Type implementedInterface in typeInfo.ImplementedInterfaces.Where(x => x.IsGenericType && x.IsGenericTypeDefinition == false))
            yield return implementedInterface;
    }

    private static IEnumerable<Type> GetValueTypesIfTypeImplementsKnownInterface(TypeInfo typeInfo)
    {
        IEnumerable<Type> candidateImplementedInterfaceTypes = GetCandidateImplementedInterfaceTypes(typeInfo);
        foreach (Type implementedInterfaceType in candidateImplementedInterfaceTypes)
        {
            Type genericTypeDefinition = implementedInterfaceType.GetGenericTypeDefinition();
            if (s_knownGenericInterfaceTypeDefinitions.TryGetValue(genericTypeDefinition, out Func<Type, Type>? function))
                yield return function(implementedInterfaceType);
        }
    }

    public static void ValidateCompatibility<TSource, TDestination>(ITypeConversion? typeConversion)
        => ValidateCompatibility(typeConversion, typeof(TSource), typeof(TDestination));

    public static void ValidateCompatibility(ITypeConversion? typeConversion, Type sourceValueType, Type destinationValueType)
    {
        if (sourceValueType == destinationValueType ||
            destinationValueType.IsAssignableFrom(sourceValueType))
            return;

        if (typeConversion is null)
            throw new ArgumentNullException(nameof(typeConversion), "Type conversion cannot be null if source and destination type are different.");

        if (CanConvert(typeConversion, sourceValueType, destinationValueType) == false)
            throw new ArgumentException($"The specified converter is unable to convert from {sourceValueType} to {destinationValueType}.", nameof(typeConversion));
    }
}
