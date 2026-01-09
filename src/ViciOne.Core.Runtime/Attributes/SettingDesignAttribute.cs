using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Zugriffsmethoden für Attributargumente definieren", Justification = "Die ID wird über Eigenschaft 'Id' als GUID verfügbar gemacht.")]
public sealed class SettingDesignAttribute(string id, string inputId, string outputId) : Attribute
{
    public Guid Id { get; } = Guid.Parse(id);
    public Guid InputId { get; } = Guid.Parse(inputId);
    public Guid OutputId { get; } = Guid.Parse(outputId);
}
