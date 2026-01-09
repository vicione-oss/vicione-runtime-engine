using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class RuntimeDriverInitializerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while initialization phase {InitPhaseNo} of driver '{DriverFullName}'.")]
    public static partial void DriverInitializationErrorOccured(this ILogger logger, Exception ex, byte initPhaseNo, string? driverFullName);
}
