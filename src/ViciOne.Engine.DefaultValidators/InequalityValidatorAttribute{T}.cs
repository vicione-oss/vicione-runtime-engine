using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Engine.DefaultValidators;

public class InequalityValidatorAttribute<T>(T? invalidValue, T? replacementValue) : ValidatorAttribute<T>
{
    public object? InvalidValue => Validator.InvalidValue;
    public object? ReplacementValue => Validator.ReplacementValue;
    public override InequalityValidator<T> Validator { get; } = new InequalityValidator<T>()
    {
        InvalidValue = invalidValue,
        ReplacementValue = replacementValue,
    };
}
