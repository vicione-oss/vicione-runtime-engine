using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class AggregatingIncomingInputLinkGroup<TValue>(IRuntimeConnector<TValue> connector, IRuntimeLink<TValue>[] links) :
    IncomingInputLinkGroup<TValue, TValue>(connector, links)
{
    private readonly IAggregatingPooling<TValue> _crossSourceAggregatingPooling = GetCrossSourceAggregatingPooling(connector.Connector as IPoolable);

    private static IAggregatingPooling<TValue> GetCrossSourceAggregatingPooling(IPoolable? poolable)
        => poolable?.CrossSourceAggregatingPooling as IAggregatingPooling<TValue> ??
            throw new InvalidOperationException($"Invalid cross source pooling configuration.");

    protected override TValue? PoolValidLinks(IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
    {
        List<TValue?> linkValues = [];
        foreach (IRuntimeLink<TValue> link in validLinks)
        {
            if (link is IQueueingRuntimeLink<TValue> queueingLink)
            {
                if (PerSourceAggregatingPooling is not null)
                {
                    TValue?[] queuedValues = queueingLink.GetAndClearQueuedValues();
                    if (queuedValues.Length > 0)
                        queueingLink.StoreValue(PerSourceAggregatingPooling.Aggregate(queuedValues));
                }
                else if (queueingLink.TryDequeueValue(out TValue? dequeued))
                    queueingLink.StoreValue(dequeued);
            }

            linkValues.Add(link.Value);
        }

        createdValue = linkValues.Count > 0;
        return createdValue
            ? _crossSourceAggregatingPooling.Aggregate(linkValues)
            : default;
    }
}
