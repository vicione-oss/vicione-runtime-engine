using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Dataflow.Pooling;

public sealed class PoolingFactory(IEnumerable<Assembly> assemblies) : TypeFactoryBase(assemblies)
{
    public IEnumerable<Type> GetAvailablePoolingTypes<T>()
        => GetAvailableTypes<T>(Poolings.GetAvailablePoolingTypes<T>);

    public IEnumerable<Type> GetAvailablePoolingTypes(Type valueType)
        => GetAvailableTypes(Poolings.GetAvailablePoolingTypes, valueType);

    public IAggregatingPooling<T>? GetNewDefaultPooling<T>()
        => GetNewDefaultInstance<T, IAggregatingPooling<T>>(Poolings.GetAvailablePoolingTypes<T>, Poolings.GetDefaultPoolingType<T>);

    public IAggregatingPooling? GetNewDefaultPooling(Type valueType)
        => GetNewDefaultInstance<IAggregatingPooling>(Poolings.GetAvailablePoolingTypes, Poolings.GetDefaultPoolingType, valueType);

    public static IAggregatingPooling<T> GetNewPooling<T>(Type poolingType)
        => GetNewInstance<IAggregatingPooling<T>>(poolingType);

    public static IAggregatingPooling GetNewPooling(Type poolingType)
        => GetNewInstance<IAggregatingPooling>(poolingType);
}
