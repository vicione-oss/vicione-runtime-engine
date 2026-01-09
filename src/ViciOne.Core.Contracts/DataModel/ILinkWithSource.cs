namespace ViciOne.Core.Contracts.DataModel;

public interface ILinkWithSource : ILink
{
    IConnector Source { get; }
}
