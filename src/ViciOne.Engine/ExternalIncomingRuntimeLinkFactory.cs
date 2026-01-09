using System.Reflection;
using ViciOne.Core.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Engine.Extensions;

namespace ViciOne.Engine;

internal sealed class ExternalIncomingRuntimeLinkFactory : GenericFactory
{
    public ExternalIncomingRuntimeLinkFactory() : base(GetGenericCreateMethod()) { }

    public IRuntimeLink Create(IExternalIncomingLink link)
        => Create<IRuntimeLink>([link],
            DataTypeCompatibilityValidator.DetermineExternalIncomingLinkSourceType(link),
            DataTypeCompatibilityValidator.DetermineValueType(link),
            DataTypeCompatibilityValidator.DetermineValueType(link.Destination));

    private static IRuntimeLink Create<TSource, TValue, TDestination>(IExternalIncomingLink<TSource, TValue, TDestination> link)
        => link.DetermineIfRuntimeLinkNeedsQueue()
            ? new ExternalIncomingQueueingRuntimeLink<TSource, TValue, TDestination>(link, link.DefaultValue)
            : new ExternalIncomingRuntimeLink<TSource, TValue, TDestination>(link, link.DefaultValue);

    private static MethodInfo GetGenericCreateMethod()
        => typeof(ExternalIncomingRuntimeLinkFactory).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
}
