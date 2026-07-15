using Microsoft.Extensions.Logging;

namespace ViciOne.Engine;

internal static partial class QueueingIncomingInputLinkGroupLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Cross-source queue overflow detected. Dropped oldest entry (total capacity: {Capacity}, total drops: {TotalDroppedCount}). Consider increasing {ChangeOptionRecommendation}.")]
    public static partial void CrossSourceQueueOverflow(this ILogger logger, int capacity, long totalDroppedCount, string changeOptionRecommendation);
}
