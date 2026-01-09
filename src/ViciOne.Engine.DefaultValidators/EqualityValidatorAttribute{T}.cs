using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Engine.DefaultValidators;

public class EqualityValidatorAttribute<T>(T? validValue, T? replacementValue) : ValidatorAttribute<T>
{
    public T? ReplacementValue => Validator.ReplacementValue;
    public override EqualityValidator<T> Validator { get; } = new()
    {
        ReplacementValue = replacementValue,
        ValidValue = validValue,
    };
    public T? ValidValue => Validator.ValidValue;
}
