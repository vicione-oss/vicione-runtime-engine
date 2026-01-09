namespace ViciOne.Core.Contracts.DataModel;

public interface IPersistentVariable<TValue> : IPersistentVariable, IPersistable<TValue>
{
    new IPersistentVariableDesign<TValue> Design { get; }
    new TValue? Value { get; set; }
}
