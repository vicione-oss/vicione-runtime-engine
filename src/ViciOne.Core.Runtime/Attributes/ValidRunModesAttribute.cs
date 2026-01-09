using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ValidRunModesAttribute(params RunMode[] validRunModes) : Attribute
{
    public RunMode[] ValidRunModes { get; } = validRunModes;
}
