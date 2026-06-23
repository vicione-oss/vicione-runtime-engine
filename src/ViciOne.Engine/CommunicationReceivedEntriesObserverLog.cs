using Microsoft.Extensions.Logging;

namespace ViciOne.Engine;

internal static partial class CommunicationReceivedEntriesObserverLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Communication receive buffer overflow detected. Dropped {DroppedCount} entries (capacity: {Capacity}). Consider increasing {ChangeOptionRecommendation}.")]
    public static partial void CommunicationReceiveBufferOverflow(this ILogger logger, long droppedCount, int capacity, string changeOptionRecommendation);
}
