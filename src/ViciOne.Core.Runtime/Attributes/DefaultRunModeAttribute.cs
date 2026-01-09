using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class DefaultRunModeAttribute(RunMode runMode) : Attribute
{
    public virtual RunMode RunMode { get; } = runMode;
}
