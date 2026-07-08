using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class ExternalIncomingQueueingRuntimeLink<TSource, TValue, TDestination>(IExternalIncomingLink<TSource, TValue, TDestination> link, object? value, int queueCapacity) :
    QueueingRuntimeLink<TSource, TValue>(link, value, queueCapacity)
{
    public new IExternalIncomingLink<TSource, TValue, TDestination> Link => (IExternalIncomingLink<TSource, TValue, TDestination>)base.Link;
}
