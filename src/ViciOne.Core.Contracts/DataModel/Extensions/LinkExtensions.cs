using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

public static class LinkExtensions
{
    public static IEnumerable<IConnector> GetConnectedConnectors(this ILink link)
    {
        if (link is ILinkWithSource linkWithSource)
            yield return linkWithSource.Source;

        if (link is ILinkWithDestination linkWithDestination)
            yield return linkWithDestination.Destination;
    }
}
