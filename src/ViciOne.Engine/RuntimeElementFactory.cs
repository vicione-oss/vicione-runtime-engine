using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class RuntimeElementFactory<TInput, TDefinition, TOutput>(IEnumerable<Assembly> assemblies) where TDefinition : notnull
{
    private readonly IEnumerable<Assembly> _assemblies = assemblies;
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
            // falls das Erstellen einer Instanz derselben Typ-Definition schon einmal nicht funktioniert hat, dann wird das Erstellen nicht erneut versucht
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
            // nur Assemblies durchsuchen, die auf irgend einer Ebene eine Verweis auf ein ViciOne-Assembly haben
            .Where(a => a.GetReferencedAssemblies().Any(e => e.FullName.Contains("ViciOne", StringComparison.Ordinal)))
            // alle Typen zusammensuchen
            .SelectMany(a => a.GetExportedTypes())
            .Select(t => t.GetTypeInfo())
            // auf brauchbares einschränken
            .Where(t => t.IsPublic && t.IsClass && t.IsAbstract == false)
            // nur Typen raussuchen, die das Ziel-Interface implementieren
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
        _availableRuntimeElementTypes ??= GetAvailableTypes(_assemblies);

        // Typ suchen, der dem Prädikat entspricht
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
