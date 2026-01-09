namespace ViciOne.Engine.DefaultValidators;

public class EqualityValidator<T> : EqualityValidatorBase<T>
{
    protected override T? CheckValue => ValidValue;
    protected override bool InvalidEquationResult => false;
    public T? ValidValue { get; set; }
}
