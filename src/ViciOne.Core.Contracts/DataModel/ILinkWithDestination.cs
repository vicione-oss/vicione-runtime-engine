namespace ViciOne.Core.Contracts.DataModel;

public interface ILinkWithDestination : ILink
{
    IConnector Destination { get; }
}
