using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Core.Runtime.Attributes;

public abstract class ValidatorAttribute<TValue> : ValidatorAttribute
{
    public abstract override IValidator<TValue> Validator { get; }
}
