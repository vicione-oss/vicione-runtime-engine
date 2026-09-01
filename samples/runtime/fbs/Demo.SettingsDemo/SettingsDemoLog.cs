using Microsoft.Extensions.Logging;

namespace Demo;

internal static partial class SettingsDemoLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "The setting '{BlockName}.{SettingName}' was changed by the block itself to '{Value}'")]
    public static partial void SettingValueChanged(this ILogger logger, string blockName, string settingName, object? value);
}
