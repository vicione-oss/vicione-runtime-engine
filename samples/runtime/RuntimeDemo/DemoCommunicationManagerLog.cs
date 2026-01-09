using Microsoft.Extensions.Logging;
using System;
using ViciOne.Core.Contracts.DataModel;

namespace RuntimeDemo;

internal static partial class DemoCommunicationManagerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{ConnectorFullName} ({ConnectorType}) geändert: {{ Time: {Time}, Valid: {Validity}, Value: {Value} }}")]
    public static partial void KnownOutgoingLinkChanged(this ILogger logger, string connectorFullName, string connectorType, DateTime time, Validity validity, object? value);

    [LoggerMessage(Level = LogLevel.Error, Message = "Beim Verarbeiten des externen Ausgangs-Links mit der ID '{LinkId}' ist ein Fehler aufgetreten.")]
    public static partial void OutgoingLinkHandlingErrorOccured(this ILogger logger, Exception ex, Guid linkId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Beim Lesen der Kommunikations-Änderung aus der Datei '{FileName}' is ein Fehler aufgetreten: {ExceptionMessage}")]
    public static partial void ReadCommunicationFileErrorOccured(this ILogger logger, Exception ex, string? fileName, string exceptionMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Link von unbekanntem Konnektor geändert: {{ Time: {Time}, Link-ID: {LinkId}, Valid: {Validity}, Value: {Value} }}")]
    public static partial void UnknownOutgoingLinkChanged(this ILogger logger, DateTime time, Guid linkId, Validity validity, object? value);
}
