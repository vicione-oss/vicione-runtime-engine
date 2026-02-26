using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;

namespace ViciOne.Core.Runtime.Execution;

public static class AssemblyLoader
{
    private static List<Assembly> CreateDefaultAdditionalAssemblies()
    {
        Assembly poolingAssembly = typeof(AndPooling).Assembly;
        Assembly validatorAssembly = typeof(RangeValidator<>).Assembly;
        List<Assembly> list = new() { poolingAssembly };
        if (poolingAssembly != validatorAssembly)
            list.Add(validatorAssembly);
        return list;
    }

    public static RelevantAssemblies CreateRelevantAssemblies(string fbAssemblyPath, AssemblyLoadContext? loadContext = null)
    {
        loadContext ??= AssemblyLoadContext.Default;
        Assembly fbAssembly = loadContext.LoadFromAssemblyPath(fbAssemblyPath);
        return CreateRelevantAssemblies(fbAssembly);
    }

    public static RelevantAssemblies CreateRelevantAssemblies(Assembly fbAssembly)
    {
        List<Assembly> additionalAssemblies = CreateDefaultAdditionalAssemblies();
        return new RelevantAssemblies(fbAssembly, additionalAssemblies);
    }
}
