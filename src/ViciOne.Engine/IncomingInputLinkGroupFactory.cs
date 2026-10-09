using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

public sealed class IncomingInputLinkGroupFactory(int queueCapacityPerLink, ILogger logger) : GenericFactory(GetGenericCreateMethod())
{
    [Obsolete("Use the constructor that takes the queue capacity per link and a logger. This one uses the default capacity and does not log dropped entries.")]
    public IncomingInputLinkGroupFactory() : this(EngineQueueOptions.DefaultCrossSourceQueueCapacityPerLink, NullLogger.Instance) { }

    public IIncomingInputLinkGroup Create(IRuntimeConnector connector, IEnumerable<IRuntimeLink> links, Func<IPoolingValidator> getPoolingValidator)
        => Create<IIncomingInputLinkGroup>([connector, links, getPoolingValidator, queueCapacityPerLink, logger],
            DataTypeCompatibilityValidator.DetermineLinkValueType(connector),
            DataTypeCompatibilityValidator.DetermineValueType(connector));

    private static IIncomingInputLinkGroup Create<TLinkValue, TInput>(IRuntimeConnector<TInput> connector, IEnumerable<IRuntimeLink> links, Func<IPoolingValidator> getPoolingValidator, int queueCapacityPerLink, ILogger logger)
    {
        PoolingStrategy strategy = ((IPoolable)connector.Connector).CrossSourcePoolingStrategy;
        IRuntimeLink<TLinkValue>[] genericLinks = [.. links.Cast<IRuntimeLink<TLinkValue>>()];
        return strategy switch
        {
            PoolingStrategy.Queue => new QueueingIncomingInputLinkGroup<TLinkValue>((IRuntimeConnector<TLinkValue>)connector, genericLinks, getPoolingValidator, queueCapacityPerLink, logger),
            PoolingStrategy.Concat => new ConcatenatingIncomingInputLinkGroup<TLinkValue>((IRuntimeConnector<TLinkValue?[]>)connector, genericLinks),
            PoolingStrategy.Aggregate => new AggregatingIncomingInputLinkGroup<TLinkValue>((IRuntimeConnector<TLinkValue>)connector, genericLinks),
            _ => throw new NotImplementedException($"{nameof(PoolingStrategy)} '{strategy}' is not implemented."),
        };
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(IncomingInputLinkGroupFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
