using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class OutgoingInputLinkGroup<TInput>(IRuntimeConnector<TInput> connector, IRuntimeLink[] links) :
    OutgoingLinkGroup<TInput>(connector, links), IOutgoingInputLinkGroup
{
    public void TransferValueToLinks(IReferenceTypeCloneHandler? cloneHandler)
        => TransferValueToLinks(cloneHandler, Connector.Value);
}
