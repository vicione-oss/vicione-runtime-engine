using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ViciOne.Core.Dataflow;

internal static class TypeFilter
{
    public static IEnumerable<Type> GetAvailableInterfaceImplementingTypes(Type unboundInterfaceType, Type interfaceType, IEnumerable<Assembly> assemblies)
    {
        IEnumerable<TypeInfo> typesImplementingUnbound = assemblies
            .Where(a => !a.IsDynamic)
            // only search assemblies that reference a ViciOne assembly at some level
            .Where(a => a.GetReferencedAssemblies().Any(e => e.FullName.Contains("ViciOne", StringComparison.Ordinal)))
            // collect all types
            .SelectMany(a => a.GetExportedTypes())
            .Select(t => t.GetTypeInfo())
            // narrow down to the usable ones
            .Where(t => (t.IsPublic || t.IsNestedPublic) && t.IsClass && t.IsAbstract == false)
            // pick only the types that implement the given interface
            // -> so that we also get generic interface implementations,
            //    for now we can only check against the unbound interface type
            .Where(t => t.ImplementedInterfaces
                .Where(ift => ift.IsGenericType && ift.IsGenericTypeDefinition == false)
                .Select(ift => ift.GetGenericTypeDefinition())
                .Contains(unboundInterfaceType));

        var matchingTypes = new List<Type>();
        Type typeToAdd;
        // it still has to be checked whether the generic types can actually be created with the concrete type parameters
        foreach (Type type in typesImplementingUnbound)
            try
            {
                // if it is a generic type, we try to create a generic type with the concrete type parameters
                if (type.IsGenericType)
                    typeToAdd = type.MakeGenericType(interfaceType.GetGenericArguments());
                // if not, we check whether the type also satisfies the bound interface
                else if (interfaceType.IsAssignableFrom(type))
                    typeToAdd = type;
                else
                    continue;

                // we try to create an instance
                Activator.CreateInstance(typeToAdd);
                // that worked, so the type is a match
                matchingTypes.Add(typeToAdd);
            }
            // if something goes wrong, we don't care
            catch { }

        return matchingTypes;
    }
}
