using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class RuntimeEngineLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while disposing driver '{DriverFullName}")]
    public static partial void DriverDisposalErrorOccured(this ILogger logger, Exception ex, string? driverFullName);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while disposing FB '{FunctionBlockId}' ({FunctionBlockName}) with type " +
                        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}'.")]
    public static partial void FunctionBlockDisposalErrorOccured(this ILogger logger, Exception ex,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);
}
