using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class SettingsPersistenceProcessorLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while applying loaded value of setting '{SettingId}' ({SettingName}).")]
    public static partial void ApplyLoadedSettingValueErrorOccured(this ILogger logger, Exception ex, Guid settingId, string settingName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Can't find setting '{SettingId}'. Applying loaded value will be skipped for this setting.")]
    public static partial void NoSettingWithIdFound(this ILogger logger, Guid settingId);
}
