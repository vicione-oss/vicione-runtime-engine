namespace ViciOne.Core.Contracts.DataModel;

public interface IValidator
{
    object? Validate(object? value, bool throwIfInvalid);
}
