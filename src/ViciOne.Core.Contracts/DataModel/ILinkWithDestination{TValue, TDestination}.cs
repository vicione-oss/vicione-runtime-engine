namespace ViciOne.Core.Contracts.DataModel;

public interface ILinkWithDestination<TValue, TDestination> : ILinkWithDestination, ILink<TValue>
{
    new IConnector<TDestination> Destination { get; }
}
