using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Zugriffsmethoden für Attributargumente definieren", Justification = "Die ID wird über Eigenschaft 'Id' als GUID verfügbar gemacht.")]
public sealed class OutputConnectorDesignAttribute(string id) : Attribute
{
    public Guid Id { get; } = Guid.Parse(id);
}
