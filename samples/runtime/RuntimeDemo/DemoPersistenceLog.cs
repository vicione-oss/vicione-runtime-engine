using Microsoft.Extensions.Logging;
using System;

namespace RuntimeDemo;

internal static partial class DemoCommunicationManagerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while loading the stored settings.")]
    public static partial void ReadSettingsFileErrorOccured(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while saving the settings.")]
    public static partial void WriteSettingsFileErrorOccured(this ILogger logger, Exception ex);
}
