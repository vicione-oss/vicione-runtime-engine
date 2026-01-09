using Microsoft.Extensions.Logging;

namespace Demo;

internal static partial class SettingsDemoLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Die Einstellung '{BlockName}.{SettingName}' wurde vom Block selbst geändert zu '{Value}'")]
    public static partial void SettingValueChanged(this ILogger logger, string blockName, string settingName, object? value);
}
