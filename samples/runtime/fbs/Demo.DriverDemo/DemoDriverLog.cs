using Microsoft.Extensions.Logging;

namespace Demo;

internal static partial class DemoDriverLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = $"{nameof(DemoDriver)}: Timer gestartet")]
    public static partial void DemoDriverTimerStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = $"{nameof(DemoDriver)}: Timer gestoppt")]
    public static partial void DemoDriverTimerStopped(this ILogger logger);
}
