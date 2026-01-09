using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public abstract class ValidatorAttribute : Attribute
{
    public abstract IValidator Validator { get; }
}
