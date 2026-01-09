using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public class DefaultConnectorValueTransferInterceptionResult<TValue> : IConnectorValueTransferInterceptionResult<TValue>
{
    public bool CancelValueTransfer { get; set; }
    public TValue? Value { get; set; }
}
