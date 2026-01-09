namespace ViciOne.Core.Contracts.Runtime;

public interface IConnectorValueTransferInterceptionResult<TValue> : IConnectorValueTransferInterceptionResult
{
    TValue? Value { get; }
}
