namespace ViciOne.Core.Contracts.Runtime;

public interface IPersistenceEntry<TValue> : IPersistenceEntry
{
    new TValue? Value { get; }
}
