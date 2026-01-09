namespace ViciOne.Core.Contracts.DataModel;

public interface IInternalLink<TSource, TValue, TDestination> : IInternalLink, ILinkWithDestination<TValue, TDestination>, ILinkWithSource<TSource>
{

}
