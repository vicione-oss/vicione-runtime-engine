using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class ExternalIncomingRuntimeLink<TSource, TValue, TDestination>(IExternalIncomingLink<TSource, TValue, TDestination> link, object? value) :
    RuntimeLink<TSource, TValue>(link, value)
{
    public new IExternalIncomingLink<TSource, TValue, TDestination> Link => (IExternalIncomingLink<TSource, TValue, TDestination>)base.Link;
}
