using System;
using System.Collections.Generic;
using System.Reflection;

namespace ViciOne.Core.Dataflow;

public abstract class TypeFactoryBase(IEnumerable<Assembly> assemblies)
{
    public delegate IEnumerable<Type> GetAvailableTypesSelector<TInput>(IEnumerable<Assembly> assemblies);
    public delegate IEnumerable<Type> GetAvailableTypesSelector(IEnumerable<Assembly> assemblies, Type inputType);
    public delegate Type? GetDefaultTypeSelector<TInput>(IEnumerable<Type> availableTypes);
    public delegate Type? GetDefaultTypeSelector(IEnumerable<Type> availableTypes, Type inputType);

    private readonly Dictionary<Type, IEnumerable<Type>> _availableTypesCache = [];
    private readonly Dictionary<Type, Type?> _defaultTypesCache = [];

    public IEnumerable<Type> GetAvailableTypes<TInput>(GetAvailableTypesSelector<TInput> getAvailableTypesSelector)
        => GetAvailableTypes((assemblies, inputType) => getAvailableTypesSelector(assemblies), typeof(TInput));

    public IEnumerable<Type> GetAvailableTypes(GetAvailableTypesSelector getAvailableTypesSelector, Type inputType)
    {
        IEnumerable<Type> returnValue;
        if (_availableTypesCache.TryGetValue(inputType, out IEnumerable<Type>? types))
            returnValue = types;
        else
            _availableTypesCache[inputType] = returnValue = getAvailableTypesSelector(assemblies, inputType);

        return returnValue;
    }

    public TOutput? GetNewDefaultInstance<TInput, TOutput>(
        GetAvailableTypesSelector<TInput> getAvailableTypesSelector,
        GetDefaultTypeSelector<TInput> getDefaultTypeSelector)
        => GetNewDefaultInstance<TOutput>(
            (assemblies, inputType) => getAvailableTypesSelector(assemblies),
            (assemblies, inputType) => getDefaultTypeSelector(assemblies),
            typeof(TInput));

    public TOutput? GetNewDefaultInstance<TOutput>(
        GetAvailableTypesSelector getAvailableTypesSelector,
        GetDefaultTypeSelector getDefaultTypeSelector,
        Type inputType)
    {
        Type? returnValue;
        if (_defaultTypesCache.TryGetValue(inputType, out Type? type))
            returnValue = type;
        else
        {
            IEnumerable<Type> available = GetAvailableTypes(getAvailableTypesSelector, inputType);
            _defaultTypesCache[inputType] = returnValue = getDefaultTypeSelector(available, inputType);
        }

        return returnValue is null
            ? default
            : GetNewInstance<TOutput>(returnValue);
    }

    public static TOutput GetNewInstance<TOutput>(Type type)
        => (TOutput)Activator.CreateInstance(type)!;
}
