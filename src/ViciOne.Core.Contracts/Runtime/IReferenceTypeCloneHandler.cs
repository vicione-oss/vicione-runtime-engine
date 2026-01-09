namespace ViciOne.Core.Contracts.Runtime;

public interface IReferenceTypeCloneHandler
{
    T? CloneIfNecessary<T>(T? value);
}
