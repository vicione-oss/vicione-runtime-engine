using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class QueueingIncomingInputLinkGroup<TValue> : IncomingInputLinkGroup<TValue, TValue>
{
    // TODO: Soll das konfigurierbar sein? Falls ja, wie?
    internal const int MaximumCapacityPerLink = 100;

    private readonly int _maximumCapacity;
    private readonly Queue<TValue?> _queue = new();
    private readonly Func<IPoolingValidator> _getPoolingValidator;

    public override bool NeedsFurtherProcessing =>
        _queue.Count > 0 ||
        (PerSourceAggregatingPooling is not null && Links.Any(x => x.Changed));

    public QueueingIncomingInputLinkGroup(IRuntimeConnector<TValue> connector, IRuntimeLink<TValue>[] links, Func<IPoolingValidator> getPoolingValidator) : base(connector, links)
    {
        _getPoolingValidator = getPoolingValidator;
        _maximumCapacity = MaximumCapacityPerLink * links.Length;
        if (PerSourceAggregatingPooling is null)
            foreach (IRuntimeLink<TValue> link in links)
                // TODO: Die Klasse sollte IDisposable implementieren und sich von diesem Event wieder abhängen
                link.LinkChanged += LinkChanged;
    }

    private void Enqueue(TValue? item)
    {
        _queue.Enqueue(item);
        if (_queue.Count > _maximumCapacity)
            _queue.Dequeue();
    }

    private void LinkChanged(object? sender, EventArgs e)
        => LinkChanged((IRuntimeLink<TValue>)sender!, _getPoolingValidator());

    private void LinkChanged(IRuntimeLink<TValue> link, IPoolingValidator validator)
    {
        if (validator.IsLinkValid(link))
            Enqueue(link.Value);
        link.GetAndResetChanged();
    }

    protected override TValue? PoolValidLinks(IEnumerable<IRuntimeLink<TValue>> validLinks, out bool createdValue)
    {
        if (PerSourceAggregatingPooling is not null)
            foreach (IQueueingRuntimeLink<TValue> link in validLinks.OfType<IQueueingRuntimeLink<TValue>>())
            {
                TValue?[] queuedValues = link.GetAndClearQueuedValues();
                if (queuedValues.Length > 0)
                {
                    link.StoreValue(PerSourceAggregatingPooling.Aggregate(queuedValues));
                    LinkChanged(link, _getPoolingValidator());
                }
            }

        createdValue = _queue.Count > 0;
        return createdValue
            ? _queue.Dequeue()
            : default;
    }
}
