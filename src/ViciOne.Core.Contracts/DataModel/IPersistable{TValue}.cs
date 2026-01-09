namespace ViciOne.Core.Contracts.DataModel;

public interface IPersistable<TValue> : IPersistable
{
    TValue? Value { get; }
}
