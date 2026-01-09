using System;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultValidators;

public class RangeValidator<T> : IValidator<T> where T : IComparable<T?>
{
    public T? MaxValue { get; set; }
    public T? MinValue { get; set; }

    public virtual T? Validate(T? value, bool throwIfInvalid)
    {
        if (value?.CompareTo(MinValue) < 0)
        {
            value = throwIfInvalid
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : MinValue;
        }

        if (value?.CompareTo(MaxValue) > 0)
        {
            value = throwIfInvalid
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : MaxValue;
        }

        return value;
    }

    object? IValidator.Validate(object? value, bool throwIfInvalid)
        => Validate((T?)value, throwIfInvalid);
}
