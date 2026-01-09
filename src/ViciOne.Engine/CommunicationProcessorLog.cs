using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class CommunicationProcessorLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while processing external output link '{LinkId}' from '{ConnectorId}' ({ConnectorName}).")]
    public static partial void ExternalOutputLinkProcessingErrorOccured(this ILogger logger, Exception ex, Guid linkId, Guid connectorId, string connectorName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No according Link was found for the received value with ID '{LinkId}'. This value will be skipped.")]
    public static partial void NoLinkFoundForReceivedId(this ILogger logger, Guid linkId);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while transferring received value with ID '{LinkId}' to the according Link.")]
    public static partial void TransferReceivedValueToLinkErrorOccured(this ILogger logger, Exception ex, Guid linkId);
}
