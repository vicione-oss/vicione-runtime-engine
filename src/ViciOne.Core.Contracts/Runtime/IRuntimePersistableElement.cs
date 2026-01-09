namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimePersistableElement : IRuntimePersistable
{
    bool Changed { get; }

    bool GetAndResetChanged();
    void MarkAsChanged();
}
