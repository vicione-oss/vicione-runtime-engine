using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Engine;

internal sealed class ExternalOutgoingRuntimeLinkFactory : GenericFactory
{
    public ExternalOutgoingRuntimeLinkFactory() : base(GetGenericCreateMethod()) { }

    public IRuntimeLink Create(IExternalOutgoingLink link, IDictionary<Guid, IRuntimeConnector> connectedConnectors)
        => Create<IRuntimeLink>([link, connectedConnectors],
            DataTypeCompatibilityValidator.DetermineValueType(link.Source));

    private static ExternalOutgoingRuntimeLink<TSource> Create<TSource>(IExternalOutgoingLink<TSource> link, IDictionary<Guid, IRuntimeConnector> connectedConnectors)
        => new(link, connectedConnectors[link.Source.Id].Value);

    private static MethodInfo GetGenericCreateMethod()
        => typeof(ExternalOutgoingRuntimeLinkFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
