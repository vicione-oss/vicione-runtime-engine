using Microsoft.Extensions.Logging;
using System;

namespace RuntimeDemo;

internal static partial class DemoCommunicationManagerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Beim Laden der gespeicherten Einstellungen ist ein Fehler aufgetreten.")]
    public static partial void ReadSettingsFileErrorOccured(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Beim Speichern der Einstellungen ist ein Fehler aufgetreten.")]
    public static partial void WriteSettingsFileErrorOccured(this ILogger logger, Exception ex);
}
