using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public interface IOutputLinkGroup : ILinkGroup
{
    void TransferValueToLinks(IReferenceTypeCloneHandler? cloneHandler, IConnectorValueTransferInterceptor interceptor);
}
