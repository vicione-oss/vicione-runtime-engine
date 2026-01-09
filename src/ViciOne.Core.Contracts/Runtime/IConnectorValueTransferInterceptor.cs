using System;

namespace ViciOne.Core.Contracts.Runtime;

public interface IConnectorValueTransferInterceptor
{
    IConnectorValueTransferInterceptionResult<TValue> Intercept<TValue>(Guid connectorId, TValue? value);
    bool IsInterceptionPending(Guid id);
}
