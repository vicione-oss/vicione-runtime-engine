using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class ConcatenatingIncomingInputLinkGroup<TValue> : IncomingInputLinkGroup<TValue?[], TValue>
{
    private bool _lastResultWasEmpty;

    public ConcatenatingIncomingInputLinkGroup(IRuntimeConnector<TValue?[]> connector, IRuntimeLink<TValue>[] links) : base(connector, links)
        => NeedsFurtherProcessing = true;

    protected override TValue?[] PoolValidLinks(IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
    {
        NeedsFurtherProcessing = false;

        TValue?[] changedValues = [.. validLinks
            .Where(x => x.Changed)
            .SelectMany(SelectValues)];

        bool currentResultIsEmpty = changedValues.Length == 0;
        createdValue = (currentResultIsEmpty && _lastResultWasEmpty) == false;
        _lastResultWasEmpty = currentResultIsEmpty;

        return changedValues;
    }

    private static IEnumerable<TValue?> SelectValues(IRuntimeLink<TValue> link)
        => link switch
        {
            IQueueingRuntimeLink<TValue> queueingLink => queueingLink.GetAndClearQueuedValues(),
            _ => Enumerable.Repeat(link.Value, 1),
        };
}
