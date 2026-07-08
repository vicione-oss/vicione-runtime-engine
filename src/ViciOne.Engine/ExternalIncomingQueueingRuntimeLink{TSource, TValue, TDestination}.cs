using Microsoft.Extensions.Logging;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class ExternalIncomingQueueingRuntimeLink<TSource, TValue, TDestination>(IExternalIncomingLink<TSource, TValue, TDestination> link, object? value, int queueCapacity, ILogger logger) :
    QueueingRuntimeLink<TSource, TValue>(link, value, queueCapacity, logger)
{
    public new IExternalIncomingLink<TSource, TValue, TDestination> Link => (IExternalIncomingLink<TSource, TValue, TDestination>)base.Link;
}
