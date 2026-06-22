using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public sealed class IncomingInputLinkGroupFactory() : GenericFactory(GetGenericCreateMethod())
{
    public IIncomingInputLinkGroup Create(IRuntimeConnector connector, IEnumerable<IRuntimeLink> links, Func<IPoolingValidator> getPoolingValidator)
        => Create<IIncomingInputLinkGroup>([connector, links, getPoolingValidator],
            DataTypeCompatibilityValidator.DetermineLinkValueType(connector),
            DataTypeCompatibilityValidator.DetermineValueType(connector));

    private static IIncomingInputLinkGroup Create<TLinkValue, TInput>(IRuntimeConnector<TInput> connector, IEnumerable<IRuntimeLink> links, Func<IPoolingValidator> getPoolingValidator)
    {
        PoolingStrategy strategy = ((IPoolable)connector.Connector).CrossSourcePoolingStrategy;
        IRuntimeLink<TLinkValue>[] genericLinks = [.. links.Cast<IRuntimeLink<TLinkValue>>()];
        return strategy switch
        {
            PoolingStrategy.Queue => new QueueingIncomingInputLinkGroup<TLinkValue>((IRuntimeConnector<TLinkValue>)connector, genericLinks, getPoolingValidator),
            PoolingStrategy.Concat => new ConcatenatingIncomingInputLinkGroup<TLinkValue>((IRuntimeConnector<TLinkValue?[]>)connector, genericLinks),
            PoolingStrategy.Aggregate => new AggregatingIncomingInputLinkGroup<TLinkValue>((IRuntimeConnector<TLinkValue>)connector, genericLinks),
            _ => throw new NotImplementedException($"{nameof(PoolingStrategy)} '{strategy}' is not implemented."),
        };
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(IncomingInputLinkGroupFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
