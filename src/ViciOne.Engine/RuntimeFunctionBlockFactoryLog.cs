using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class RuntimeFunctionBlockFactoryLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while creating instance of FB-Design " +
                "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}': {ExceptionMessage} " +
                "The creation of further FBs of this type will be skipped.")]
    public static partial void FunctionBlockInstanceCreationErrorOccured(this ILogger logger, Exception ex, string functionBlockDesignNamespace, string functionBlockDesignName, string exceptionMessage);
}
