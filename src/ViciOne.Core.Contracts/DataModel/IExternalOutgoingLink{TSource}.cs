namespace ViciOne.Core.Contracts.DataModel;

public interface IExternalOutgoingLink<TSource> : IExternalOutgoingLink, ILinkWithSource<TSource>, ILink<TSource>
{

}
