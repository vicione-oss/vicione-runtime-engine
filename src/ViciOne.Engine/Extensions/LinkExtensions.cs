using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Engine.Extensions;

internal static class LinkExtensions
{
    extension(ILinkWithDestination link)
    {
        internal bool DetermineIfRuntimeLinkNeedsQueue()
        {
            if (link.Destination is not IPoolable poolableDestination)
                return false;

            if (poolableDestination.CrossSourcePoolingStrategy != PoolingStrategy.Queue ||
                poolableDestination.PerSourcePoolingStrategy != PoolingStrategy.Queue)
                return true;

            return false;
        }
    }
}
