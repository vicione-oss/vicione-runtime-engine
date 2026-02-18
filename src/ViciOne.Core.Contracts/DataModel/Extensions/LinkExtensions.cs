using System.Collections.Generic;

namespace ViciOne.Core.Contracts.DataModel.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class LinkExtensions
{
    extension(ILink link)
    {
        public IEnumerable<IConnector> GetConnectedConnectors()
        {
            if (link is ILinkWithSource linkWithSource)
                yield return linkWithSource.Source;

            if (link is ILinkWithDestination linkWithDestination)
                yield return linkWithDestination.Destination;
        }
    }
}
