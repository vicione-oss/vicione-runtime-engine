using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultValidators;

public class StringLengthValidator : IValidator<string>
{
    public uint MaxLength { get; set; } = int.MaxValue;
    public uint MinLength { get; set; }

    public virtual string? Validate(string? value, bool throwIfInvalid)
    {
        if (value is null)
        {
            if (MinLength == 0)
                return value;
            else
            {
                return throwIfInvalid
                    ? throw new ArgumentNullException(nameof(value))
                    : value;
            }
        }

        if (value.Length < MinLength)
        {
            return throwIfInvalid
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : value.PadRight((int)MinLength);
        }

        if (value.Length > MaxLength)
        {
            return throwIfInvalid
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : value[..(int)MaxLength];
        }

        return value;
    }

    object? IValidator.Validate(object? value, bool throwIfInvalid)
        => Validate((string?)value, throwIfInvalid);
}
