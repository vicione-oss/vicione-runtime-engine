using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultValidators;

public class EnumValidator<T> : IValidator<T> where T : Enum
{
    public T? ReplacementValue { get; set; }

    public T? Validate(T? value, bool throwIfInvalid)
    {
        if (value is null || Enum.IsDefined(typeof(T), value) == false)
        {
            return throwIfInvalid
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : ReplacementValue;
        }

        return value;
    }

    object? IValidator.Validate(object? value, bool throwIfInvalid)
        => Validate((T?)value, throwIfInvalid);
}
