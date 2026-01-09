namespace ViciOne.Engine.DefaultValidators;

public class InequalityValidator<T> : EqualityValidatorBase<T>
{
    protected override T? CheckValue => InvalidValue;
    protected override bool InvalidEquationResult => true;
    public T? InvalidValue { get; set; }
}
