using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class RuntimeDriverFactoryLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while creating instance of driver '{DriverTypeName}': {ExceptionMessage}")]
    public static partial void DriverInstanceCreationErrorOccured(this ILogger logger, Exception ex, string driverTypeName, string exceptionMessage);
}
