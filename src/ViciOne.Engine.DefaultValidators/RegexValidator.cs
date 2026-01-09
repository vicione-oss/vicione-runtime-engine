using System;
using System.Text.RegularExpressions;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultValidators;

public partial class RegexValidator : IValidator<string>
{
    private const string DefaultPattern = "^.*$";
    [GeneratedRegex(DefaultPattern)]
    private static partial Regex DefaultRegex();

    private string _pattern = DefaultPattern;
    private Regex _regex = DefaultRegex();

    public string Pattern
    {
        get => _pattern;
        set
        {
            _regex = new Regex(value);
            _pattern = value;
        }
    }
    public string? ReplacementValue { get; set; } = string.Empty;

    public string? Validate(string? value, bool throwIfInvalid)
    {
        if (value is null && throwIfInvalid == false)
            return ReplacementValue;

        if (value is null || _regex.IsMatch(value) == false)
            return throwIfInvalid
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : ReplacementValue;

        return value;
    }

    object? IValidator.Validate(object? value, bool throwIfInvalid)
        => Validate((string?)value, throwIfInvalid);
}
