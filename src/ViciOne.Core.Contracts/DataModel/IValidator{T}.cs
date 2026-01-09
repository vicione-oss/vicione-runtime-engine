namespace ViciOne.Core.Contracts.DataModel;

public interface IValidator<T> : IValidator
{
    T? Validate(T? value, bool throwIfInvalid);
}
