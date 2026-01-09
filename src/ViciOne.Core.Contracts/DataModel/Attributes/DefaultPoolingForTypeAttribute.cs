using System;

namespace ViciOne.Core.Contracts.DataModel.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DefaultPoolingForTypeAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}
