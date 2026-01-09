using System;
using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Engine.DefaultValidators;

public class EnumValidatorAttribute<T>(T? replacementValue) : ValidatorAttribute<T> where T : Enum
{
    public T? ReplacementValue => Validator.ReplacementValue;
    public override EnumValidator<T> Validator { get; } = new() { ReplacementValue = replacementValue };
}
