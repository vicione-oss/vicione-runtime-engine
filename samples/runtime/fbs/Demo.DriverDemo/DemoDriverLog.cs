using Microsoft.Extensions.Logging;

namespace Demo;

internal static partial class DemoDriverLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = $"{nameof(DemoDriver)}: timer started")]
    public static partial void DemoDriverTimerStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = $"{nameof(DemoDriver)}: timer stopped")]
    public static partial void DemoDriverTimerStopped(this ILogger logger);
}
