using Newtonsoft.Json.Serialization;
using System;

namespace ViciOne.Serialization.Json;

internal sealed class CrossPlatformSerializationBinder : DefaultSerializationBinder
{
    private const string DotNetCore = "System.Private.CoreLib";
    private const string FullFramework = "mscorlib";

    private static readonly string s_usedAssemblyName = typeof(double).Assembly.FullName!;

    public static CrossPlatformSerializationBinder Instance { get; } = new CrossPlatformSerializationBinder();

    private CrossPlatformSerializationBinder() { }

    public override Type BindToType(string? assemblyName, string typeName)
    {
        if (IsMscorLib(s_usedAssemblyName))
            typeName = ReplaceLib(typeName, DotNetCore, FullFramework);
        else if (IsSystemPrivateCoreLib(s_usedAssemblyName))
            typeName = ReplaceLib(typeName, FullFramework, DotNetCore);

        return base.BindToType(assemblyName, typeName);
    }

    private static bool IsMscorLib(string assemblyFullName)
        => assemblyFullName.StartsWith($"{FullFramework},", StringComparison.OrdinalIgnoreCase);

    private static bool IsSystemPrivateCoreLib(string assemblyFullName)
        => assemblyFullName.StartsWith($"{DotNetCore},", StringComparison.OrdinalIgnoreCase);

    private static string ReplaceLib(string original, string search, string replaceWith)
        => original.Replace(search, replaceWith, StringComparison.Ordinal);
}
