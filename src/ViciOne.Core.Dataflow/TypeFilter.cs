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
            // nur Assemblies durchsuchen, die auf irgend einer Ebene eine Verweis auf ein ViciOne-Assembly haben
            .Where(a => a.GetReferencedAssemblies().Any(e => e.FullName.Contains("ViciOne", StringComparison.Ordinal)))
            // alle Typen zusammensuchen
            .SelectMany(a => a.GetExportedTypes())
            .Select(t => t.GetTypeInfo())
            // auf brauchbares einschränken
            .Where(t => (t.IsPublic || t.IsNestedPublic) && t.IsClass && t.IsAbstract == false)
            // nur Typen raussuchen, die das angegebene-Interface implementieren
            // -> damit wir auch generische Interface-Implementierungen bekommen, 
            //    können wir vorerst nur auf den ungebundenen Interface-Typ prüfen
            .Where(t => t.ImplementedInterfaces
                .Where(ift => ift.IsGenericType && ift.IsGenericTypeDefinition == false)
                .Select(ift => ift.GetGenericTypeDefinition())
                .Contains(unboundInterfaceType));

        var matchingTypes = new List<Type>();
        Type typeToAdd;
        // hier muss noch geprüft werden, ob die generischen Typen auch mit den konkreten Typ-Parametern erstellt werden können
        foreach (Type type in typesImplementingUnbound)
            try
            {
                // wenn es ein generischer Typ ist, dann wird versucht, einen generischen Typ mit den konkreten Typparametern zu erzeugen
                if (type.IsGenericType)
                    typeToAdd = type.MakeGenericType(interfaceType.GetGenericArguments());
                // wenn nicht, wird geprüft, ob der Typ auch das gebundene Interface erfüllt
                else if (interfaceType.IsAssignableFrom(type))
                    typeToAdd = type;
                else
                    continue;

                // es wird versucht eine Instanz zu erzeugen
                Activator.CreateInstance(typeToAdd);
                // hat geklappt, der Typ passt
                matchingTypes.Add(typeToAdd);
            }
            // falls etwas schief läuft, dann ist es uns egal
            catch { }

        return matchingTypes;
    }
}
