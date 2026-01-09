using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

namespace ViciOne.Engine;

internal sealed class InternalRuntimeLinkFactory : GenericFactory
{
    public InternalRuntimeLinkFactory() : base(GetGenericCreateMethod()) { }

    public IRuntimeLink Create(IInternalLink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors)
        => Create<IRuntimeLink>([link, connectedConnectors],
            DataTypeCompatibilityValidator.DetermineValueType(link.Source),
            DataTypeCompatibilityValidator.DetermineValueType(link),
            DataTypeCompatibilityValidator.DetermineValueType(link.Destination));

    private static IRuntimeLink Create<TSource, TValue, TDestination>(IInternalLink<TSource, TValue, TDestination> link,
        IDictionary<Guid, IRuntimeConnector> connectedConnectors)
    {
        bool sourceIsOnThisEngine = connectedConnectors.TryGetValue(link.Source.Id, out IRuntimeConnector? sourceRuntimeConnector);
        bool destinationIsOnThisEngine = connectedConnectors.ContainsKey(link.Destination.Id);
        bool needsQueue = destinationIsOnThisEngine && link.DetermineIfRuntimeLinkNeedsQueue();

        // TODO: wenn sich der Quell-Konnektor auf einer anderen Engine befindet, haben wir keinen Zugriff darauf
        //       wir können also nur den Konnektor aus dem Dataflow nutzen, um den Link zu initialisieren
        //       das Problem dabei ist, dass sich der Wert durch einen ggf. vorhandenen Validator an der Quelle vom Wert des Runtime-Konnektors unterscheiden kann
        object? defaultValue = sourceIsOnThisEngine
            ? sourceRuntimeConnector!.Value
            : link.Source.Value;

        return needsQueue
            ? new InternalQueueingRuntimeLink<TSource, TValue, TDestination>(link, defaultValue)
            : new InternalRuntimeLink<TSource, TValue, TDestination>(link, defaultValue);
    }

    private static MethodInfo GetGenericCreateMethod()
        => typeof(InternalRuntimeLinkFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
