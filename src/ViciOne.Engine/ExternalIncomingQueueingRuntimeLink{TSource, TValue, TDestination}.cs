using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class ExternalIncomingQueueingRuntimeLink<TSource, TValue, TDestination>(IExternalIncomingLink<TSource, TValue, TDestination> link, object? value) :
    QueueingRuntimeLink<TSource, TValue>(link, value)
{
    public new IExternalIncomingLink<TSource, TValue, TDestination> Link => (IExternalIncomingLink<TSource, TValue, TDestination>)base.Link;
}
