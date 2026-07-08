using Microsoft.Extensions.Logging;

namespace ViciOne.Engine;

internal static partial class QueueingRuntimeLinkLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Per-link queue overflow detected. Dropped oldest entry (capacity: {Capacity}, total drops: {TotalDroppedCount}). Consider increasing {ChangeOptionRecommendation}.")]
    public static partial void PerLinkQueueOverflow(this ILogger logger, int capacity, long totalDroppedCount, string changeOptionRecommendation);
}
