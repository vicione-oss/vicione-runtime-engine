using System;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DefaultCycleFrequencyAttribute(uint cycleFrequency) : Attribute
{
    public virtual uint CycleFrequency { get; } = cycleFrequency;
}
