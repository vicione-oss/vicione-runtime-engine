namespace ViciOne.Core.Contracts.DataModel;

public interface IExternalIncomingLink : IExternalLink, ILinkWithDestination
{
    object? DefaultValue { get; set; }
}
