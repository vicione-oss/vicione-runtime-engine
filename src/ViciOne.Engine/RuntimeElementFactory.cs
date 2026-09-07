using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class RuntimeElementFactory<TInput, TDefinition, TOutput>(IEnumerable<Assembly> assemblies) where TDefinition : notnull
{
    private IEnumerable<Type>? _availableRuntimeElementTypes;
    private readonly Dictionary<TDefinition, Type?> _availableRuntimeElementTypesCache = [];

    protected abstract Type ExpectedUnboundGenericBaseType { get; }

    private TOutput CreateInstance(TDefinition definition)
        => CreateInstance(GetType(definition) ?? throw new TypeLoadException($"No implementing type found."));

    private static TOutput CreateInstance(Type runtimeFunctionBlockType)
        => (TOutput)Activator.CreateInstance(runtimeFunctionBlockType)!;

    protected (IEnumerable<(TInput, TOutput)> Instances, IEnumerable<(TDefinition Definition, Exception Exception)> FailedDefinitions)
        CreateInstancesBase(IEnumerable<TInput> inputObjects, IUniqueNameProvider uniqueNameProvider, ILoggerFactory loggerFactory)
    {
        var createdInstances = new List<(TInput, TOutput)>();
        var failedDefinitions = new Dictionary<TDefinition, Exception>();
        TDefinition definition;
        foreach (TInput inputObject in inputObjects)
        {
            definition = GetDefinition(inputObject);
            // if creating an instance of the same type definition has failed before, we don't try again
            if (failedDefinitions.ContainsKey(definition))
                continue;

            try
            {
                TOutput instance = CreateInstance(definition);
                ILogger logger = CreateLogger(inputObject, instance, uniqueNameProvider, loggerFactory);
                SetupInstance(inputObject, instance, uniqueNameProvider, logger);
                createdInstances.Add((inputObject, instance));
            }
            catch (Exception ex) { failedDefinitions.Add(definition, ex); }
        }
        return (createdInstances, failedDefinitions.Select(x => (x.Key, x.Value)));
    }

    protected abstract ILogger CreateLogger(TInput inputObject, TOutput instance, IUniqueNameProvider uniqueNameProvider, ILoggerFactory loggerFactory);

    private IEnumerable<Type> GetAvailableTypes(IEnumerable<Assembly> assemblies)
        => assemblies
            .Where(a => !a.IsDynamic)
            // only search assemblies that reference a ViciOne assembly at some level
            .Where(a => a.GetReferencedAssemblies().Any(e => e.FullName.Contains("ViciOne", StringComparison.Ordinal)))
            // collect all types
            .SelectMany(a => a.GetExportedTypes())
            .Select(t => t.GetTypeInfo())
            // narrow down to the usable ones
            .Where(t => t.IsPublic && t.IsClass && t.IsAbstract == false)
            // pick only the types that implement the target interface
            .Where(t =>
                t.ImplementedInterfaces.Contains(typeof(TOutput)) &&
                HasExpectedUnboundGenericBaseType(t));

    protected abstract TDefinition GetDefinition(TInput inputObject);

    private Type? GetType(TDefinition definition)
    {
        Type? returnValue;
        if (_availableRuntimeElementTypesCache.TryGetValue(definition, out Type? type))
            returnValue = type;
        else
            _availableRuntimeElementTypesCache[definition] = returnValue = GetTypeUncached(definition);

        return returnValue;
    }

    private Type? GetTypeUncached(TDefinition definition)
    {
        _availableRuntimeElementTypes ??= GetAvailableTypes(assemblies);

        // find the type that matches the predicate
        return _availableRuntimeElementTypes.FirstOrDefault(x => IsRuntimeElementType(x, definition));
    }

    private bool HasExpectedUnboundGenericBaseType(Type type)
    {
        if (type.BaseType == null)
            return false;
        else if (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == ExpectedUnboundGenericBaseType)
            return true;
        else
            return HasExpectedUnboundGenericBaseType(type.BaseType);
    }

    protected abstract bool IsRuntimeElementType(Type type, TDefinition definition);

    protected abstract void SetupInstance(TInput inputObject, TOutput instance, IUniqueNameProvider uniqueNameProvider, ILogger logger);
}
