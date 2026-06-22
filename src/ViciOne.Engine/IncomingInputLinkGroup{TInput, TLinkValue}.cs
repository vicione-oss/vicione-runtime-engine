using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal abstract class IncomingInputLinkGroup<TInput, TLinkValue>(IRuntimeConnector<TInput> connector, IRuntimeLink<TLinkValue>[] links, bool needsFurtherProcessing = false) : IIncomingInputLinkGroup
{
    public IRuntimeConnector<TInput> Connector { get; } = connector;
    IRuntimeConnector ILinkGroup.Connector => Connector;
    public IRuntimeLink<TLinkValue>[] Links { get; } = links;
    IEnumerable<IRuntimeLink> ILinkGroup.Links => Links;
    public virtual bool NeedsFurtherProcessing { get; protected set; } = needsFurtherProcessing;
    protected IAggregatingPooling<TLinkValue>? PerSourceAggregatingPooling { get; } = GetPerSourceAggregatingPooling(connector.Connector as IPoolable);

    private void DetermineAndSetConnectorValidity(IPoolingValidator validator)
    {
        Validity validity = validator.GetResultValidity(Links);
        Connector.SetValidityIfNotEqual(validity);
    }

    public void Pool(IPoolingValidator validator, IConnectorValueTransferInterceptor interceptor)
    {
        DetermineAndSetConnectorValidity(validator);

        IEnumerable<IRuntimeLink<TLinkValue>> validLinks = validator.SelectLinks(Links);
        TInput? value = PoolValidLinks(validLinks, out bool createdValue);
        if (createdValue == false)
            value = Connector.Value;

        // TODO: muss hier geprüft werden, ob der Interceptor überhaupt aufgerufen werden muss? (IsInterceptionPending(...))
        IConnectorValueTransferInterceptionResult<TInput> result = interceptor.Intercept(Connector.Connector.Id, value);
        if (result.CancelValueTransfer == false)
            Connector.SetValueIfNotEqual(result.Value);

        ResetLinkChanged(Links);
    }

    protected abstract TInput? PoolValidLinks(IEnumerable<IRuntimeLink<TLinkValue>> validLinks, out bool createdValue);

    private static void ResetLinkChanged(IEnumerable<IRuntimeLink> links)
    {
        foreach (IRuntimeLink link in links)
            link.GetAndResetChanged();
    }

    protected static IAggregatingPooling<TLinkValue>? GetPerSourceAggregatingPooling(IPoolable? poolable)
        => poolable?.PerSourcePoolingStrategy == PoolingStrategy.Aggregate
            ? poolable?.PerSourceAggregatingPooling as IAggregatingPooling<TLinkValue> ?? throw new InvalidOperationException($"Invalid per source pooling configuration.")
            : null;
}
