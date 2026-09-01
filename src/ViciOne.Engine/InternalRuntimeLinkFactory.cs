using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

namespace ViciOne.Engine;

internal sealed class InternalRuntimeLinkFactory(int queueCapacity, ILogger logger) : GenericFactory(GetGenericCreateMethod())
{
    public IRuntimeLink Create(IInternalLink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors)
        => Create<IRuntimeLink>([link, connectedConnectors, queueCapacity, logger],
            DataTypeCompatibilityValidator.DetermineValueType(link.Source),
            DataTypeCompatibilityValidator.DetermineValueType(link),
            DataTypeCompatibilityValidator.DetermineValueType(link.Destination));

    private static IRuntimeLink Create<TSource, TValue, TDestination>(IInternalLink<TSource, TValue, TDestination> link,
        IDictionary<Guid, IRuntimeConnector> connectedConnectors, int queueCapacity, ILogger logger)
    {
        bool sourceIsOnThisEngine = connectedConnectors.TryGetValue(link.Source.Id, out IRuntimeConnector? sourceRuntimeConnector);
        bool destinationIsOnThisEngine = connectedConnectors.ContainsKey(link.Destination.Id);
        bool needsQueue = destinationIsOnThisEngine && link.DetermineIfRuntimeLinkNeedsQueue();

        // TODO: if the source connector is located on another engine, we have no access to it
        //       so we can only use the connector from the dataflow to initialize the link
        //       the problem with that is that a validator possibly present at the source can make the value differ from the runtime connector's value
        object? defaultValue = sourceIsOnThisEngine
            ? sourceRuntimeConnector!.Value
            : link.Source.Value;

        return needsQueue
            ? new InternalQueueingRuntimeLink<TSource, TValue, TDestination>(link, defaultValue, queueCapacity, logger)
            : new InternalRuntimeLink<TSource, TValue, TDestination>(link, defaultValue);
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(InternalRuntimeLinkFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
