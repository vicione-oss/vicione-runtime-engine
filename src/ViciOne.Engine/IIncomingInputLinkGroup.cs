using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public interface IIncomingInputLinkGroup : ILinkGroup
{
    bool NeedsFurtherProcessing { get; }

    void Pool(IPoolingValidator validator, IConnectorValueTransferInterceptor interceptor);
}
