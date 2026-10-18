using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class AggregatingIncomingInputLinkGroup<TValue>(IRuntimeConnector<TValue> connector, IRuntimeLink<TValue>[] links) :
    IncomingInputLinkGroup<TValue, TValue>(connector, links)
{
    private readonly IAggregatingPooling<TValue> _crossSourceAggregatingPooling = GetCrossSourceAggregatingPooling(connector.Connector as IPoolable);
    // reused in every cycle, which is safe because the engine pools a link group only from its cycle
    private readonly List<TValue?> _linkValues = [];

    private static IAggregatingPooling<TValue> GetCrossSourceAggregatingPooling(IPoolable? poolable)
        => poolable?.CrossSourceAggregatingPooling as IAggregatingPooling<TValue> ??
            throw new InvalidOperationException($"Invalid cross source pooling configuration.");

    protected override TValue? PoolValidLinks(IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
    {
        _linkValues.Clear();
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

            _linkValues.Add(link.Value);
        }

        createdValue = _linkValues.Count > 0;
        return createdValue
            ? _crossSourceAggregatingPooling.Aggregate(_linkValues)
            : default;
    }
}
