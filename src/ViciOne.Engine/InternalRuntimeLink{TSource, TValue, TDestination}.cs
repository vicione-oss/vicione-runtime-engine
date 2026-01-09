using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class InternalRuntimeLink<TSource, TValue, TDestination>(IInternalLink<TSource, TValue, TDestination> link, object? value) :
    RuntimeLink<TSource, TValue>(link, value)
{
    public new IInternalLink<TSource, TValue, TDestination> Link => (IInternalLink<TSource, TValue, TDestination>)base.Link;
}
