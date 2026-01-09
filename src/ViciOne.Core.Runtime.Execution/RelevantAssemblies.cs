using System.Collections.Generic;
using System.Reflection;

namespace ViciOne.Core.Runtime.Execution;

public class RelevantAssemblies(Assembly fbAssembly, IEnumerable<Assembly> additionalAssemblies)
{
    public IEnumerable<Assembly> AdditionalAssemblies { get; } = additionalAssemblies;
    public IEnumerable<Assembly> AllAssemblies => [.. AdditionalAssemblies, FbAssembly];
    public Assembly FbAssembly { get; } = fbAssembly;
}
