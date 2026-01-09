using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine;

internal class ExternalOutgoingRuntimeLink<TSource> : RuntimeLink<TSource, TSource>
{
    public new IExternalOutgoingLink<TSource> Link => (IExternalOutgoingLink<TSource>)base.Link;

    public ExternalOutgoingRuntimeLink(IExternalOutgoingLink<TSource> link, object? value) : base(link, value)
        => MarkAsChanged();
}
