using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Zugriffsmethoden für Attributargumente definieren", Justification = "Die ID wird über Eigenschaft 'Id' als GUID verfügbar gemacht.")]
public sealed class FunctionBlockDesignAttribute(string id, string functionBlockEnabledInputId, string functionBlockEnabledOutputId) : Attribute
{
    public Guid FunctionBlockEnabledInputId { get; } = Guid.Parse(functionBlockEnabledInputId);
    public Guid FunctionBlockEnabledOutputId { get; } = Guid.Parse(functionBlockEnabledOutputId);
    public Guid Id { get; } = Guid.Parse(id);
    public string? Name { get; set; }
    public string? Namespace { get; set; }
}
