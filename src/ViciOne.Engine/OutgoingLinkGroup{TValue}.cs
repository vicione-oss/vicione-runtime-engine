using System.Collections.Generic;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class OutgoingLinkGroup<TValue>(IRuntimeConnector<TValue> connector, IRuntimeLink[] links) : ILinkGroup
{
    public IRuntimeConnector<TValue> Connector { get; set; } = connector;
    IRuntimeConnector ILinkGroup.Connector => Connector;
    public IRuntimeLink[] Links { get; set; } = links;
    IEnumerable<IRuntimeLink> ILinkGroup.Links => Links;

    protected void TransferValueToLinks(IReferenceTypeCloneHandler? cloneHandler, TValue? value)
    {
        // TODO: possibly perform value forwarding and conversion in parallel
        foreach (IRuntimeLink link in Links)
            link.SetData(cloneHandler is not null ? cloneHandler.CloneIfNecessary(value) : value, Connector.ChangedTime, Connector.Validity);
    }
}
