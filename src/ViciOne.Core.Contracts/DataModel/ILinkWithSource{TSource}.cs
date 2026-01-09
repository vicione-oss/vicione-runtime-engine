namespace ViciOne.Core.Contracts.DataModel;

public interface ILinkWithSource<TSource> : ILinkWithSource
{
    new IConnector<TSource> Source { get; }
}
