using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "The ID is made available as a GUID via the 'Id' property.")]
public sealed class PersistentVariableDesignAttribute(string id) : Attribute
{
    public Guid Id { get; } = Guid.Parse(id);
}
