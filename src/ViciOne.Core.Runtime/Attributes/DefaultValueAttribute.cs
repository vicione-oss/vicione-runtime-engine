using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DefaultValueAttribute(object? value) : Attribute
{
    public virtual object? Value { get; } = value;
}
