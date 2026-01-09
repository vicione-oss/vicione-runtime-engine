namespace ViciOne.Core.Contracts.DataModel;

public interface IPersistentVariableDesign<TValue> : IPersistentVariableDesign
{
    new TValue? DefaultValue { get; }
}
