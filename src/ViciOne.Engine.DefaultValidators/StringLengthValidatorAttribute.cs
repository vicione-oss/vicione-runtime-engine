using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Engine.DefaultValidators;

public class StringLengthValidatorAttribute(uint minLength, uint maxLength) : ValidatorAttribute<string>
{
    public uint MaxLength => Validator.MaxLength;
    public uint MinLength => Validator.MinLength;
    public override StringLengthValidator Validator { get; } = new StringLengthValidator()
    {
        MinLength = minLength,
        MaxLength = maxLength,
    };
}
