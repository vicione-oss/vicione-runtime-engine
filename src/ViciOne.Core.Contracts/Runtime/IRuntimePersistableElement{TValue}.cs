namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistableElement<TValue> : IRuntimePersistableElement
{
    bool SetValueIfNotEqual(TValue? value);
}
