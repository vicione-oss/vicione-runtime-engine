using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class ExternalOutgoingRuntimeLink<TSource>(IExternalOutgoingLink<TSource> link, object? value) : RuntimeLink<TSource, TSource>(link, value, true)
{
    public new IExternalOutgoingLink<TSource> Link => (IExternalOutgoingLink<TSource>)base.Link;
}
