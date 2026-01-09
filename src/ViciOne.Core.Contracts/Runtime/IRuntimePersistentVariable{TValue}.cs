namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistentVariable<TValue> : IRuntimePersistentVariable, IRuntimePersistable<TValue>, IRuntimePersistableElement<TValue>
{

}
