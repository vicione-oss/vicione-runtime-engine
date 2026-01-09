using System;
using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Engine.DefaultValidators;

public class RangeValidatorAttribute<T>(T? minValue, T? maxValue) : ValidatorAttribute<T> where T : IComparable<T?>
{
    public T? MinValue => Validator.MinValue;
    public T? MaxValue => Validator.MaxValue;
    public override RangeValidator<T> Validator { get; } = new()
    {
        MinValue = minValue,
        MaxValue = maxValue,
    };
}
