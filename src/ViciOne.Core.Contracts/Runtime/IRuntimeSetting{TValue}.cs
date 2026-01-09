namespace ViciOne.Core.Contracts.Runtime;

public interface IRuntimeSetting<TValue> : IRuntimeSetting, IRuntimePersistable<TValue>, IRuntimePersistableElement<TValue>
{
    new IRuntimeConnector<TValue> Input { get; }
    new IRuntimeConnector<TValue> Output { get; }
}
