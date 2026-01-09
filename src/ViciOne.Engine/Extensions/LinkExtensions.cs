using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.Extensions;

internal static class LinkExtensions
{
    internal static bool DetermineIfRuntimeLinkNeedsQueue(this ILinkWithDestination link)
    {
        if (link.Destination is not IPoolable poolableDestination)
            return false;

        if (poolableDestination.CrossSourcePoolingStrategy != PoolingStrategy.Queue ||
            poolableDestination.PerSourcePoolingStrategy != PoolingStrategy.Queue)
            return true;

        return false;
    }
}
