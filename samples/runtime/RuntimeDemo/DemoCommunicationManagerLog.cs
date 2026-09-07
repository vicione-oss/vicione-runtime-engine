using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;

namespace RuntimeDemo;

internal static partial class DemoCommunicationManagerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{ConnectorFullName} ({ConnectorType}) changed: {{ Time: {Time}, Valid: {Validity}, Value: {Value} }}")]
    public static partial void KnownOutgoingLinkChanged(this ILogger logger, string connectorFullName, string connectorType, DateTime time, Validity validity, object? value);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while processing the external outgoing link with the ID '{LinkId}'.")]
    public static partial void OutgoingLinkHandlingErrorOccured(this ILogger logger, Exception ex, Guid linkId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while reading the communication change from the file '{FileName}': {ExceptionMessage}")]
    public static partial void ReadCommunicationFileErrorOccured(this ILogger logger, Exception ex, string? fileName, string exceptionMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Link from unknown connector changed: {{ Time: {Time}, Link-ID: {LinkId}, Valid: {Validity}, Value: {Value} }}")]
    public static partial void UnknownOutgoingLinkChanged(this ILogger logger, DateTime time, Guid linkId, Validity validity, object? value);
}
