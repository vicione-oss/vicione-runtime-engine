using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class OutputLinkGroup<TOutput>(IRuntimeConnector<TOutput> connector, IRuntimeLink[] links) :
    OutgoingLinkGroup<TOutput>(connector, links), IOutputLinkGroup
{
    public void TransferValueToLinks(IReferenceTypeCloneHandler? cloneHandler, IConnectorValueTransferInterceptor interceptor)
    {
        TOutput? value = Connector.Value;
        IConnectorValueTransferInterceptionResult<TOutput> result = interceptor.Intercept(Connector.Connector.Id, value);
        if (result.CancelValueTransfer)
            return;
        value = result.Value;
        TransferValueToLinks(cloneHandler, value);
    }
}
