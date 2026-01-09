using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DriverAttribute : Attribute
{
    public virtual string TypeName { get; }

    public DriverAttribute(string typeName)
        => TypeName = typeName;

    protected DriverAttribute(Type type)
        => TypeName = type.FullName ?? throw new ArgumentException("The specified type does not provide a fully qualified type name.", nameof(type));
}
