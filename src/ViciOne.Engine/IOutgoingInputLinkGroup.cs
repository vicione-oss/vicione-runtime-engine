using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal interface IOutgoingInputLinkGroup : ILinkGroup
{
    void TransferValueToLinks(IReferenceTypeCloneHandler? cloneHandler);
}
