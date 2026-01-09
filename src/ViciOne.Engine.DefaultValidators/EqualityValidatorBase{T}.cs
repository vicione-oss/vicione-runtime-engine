using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.DefaultValidators;

public abstract class EqualityValidatorBase<T> : IValidator<T>
{
    protected abstract T? CheckValue { get; }
    protected abstract bool InvalidEquationResult { get; }
    public T? ReplacementValue { get; set; }

    public virtual T? Validate(T? value, bool throwIfInvalid)
    {
        if (EqualityComparer<T?>.Default.Equals(value, CheckValue) == InvalidEquationResult)
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
