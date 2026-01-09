using ViciOne.Core.Runtime.Attributes;

namespace ViciOne.Engine.DefaultValidators;

public class RegexValidatorAttribute(string pattern, string? replacementValue) : ValidatorAttribute<string>
{
    public string? ReplacementValue => Validator.ReplacementValue;
    public override RegexValidator Validator { get; } = new RegexValidator()
    {
        Pattern = pattern,
        ReplacementValue = replacementValue,
    };
    public string Pattern => Validator.Pattern;
}
