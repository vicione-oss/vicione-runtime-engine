using System;
using ViciOne.Core.Contracts.DataModel;

internal sealed class MockValidator<T> : IValidator<T>
{
    public T? SubstituteValue { get; set; }
    public bool ThrowOnThrowIfInvalid { get; set; }
    public bool UseSubstituteValue { get; set; }

    public T? Validate(T? value, bool throwIfInvalid)
    {
        if (throwIfInvalid && ThrowOnThrowIfInvalid)
            throw new ArgumentOutOfRangeException(nameof(value));

        return UseSubstituteValue
            ? SubstituteValue
            : value;
    }

    object? IValidator.Validate(object? value, bool throwIfInvalid)
        => Validate((T?)value, throwIfInvalid);
}
