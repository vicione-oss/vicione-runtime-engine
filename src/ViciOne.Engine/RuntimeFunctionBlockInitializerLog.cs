using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Engine;

internal static partial class RuntimeFunctionBlockInitializerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occured while initialization phase {InitPhaseNo} of FB " +
        "'{FunctionBlockId}' ({FunctionBlockName}) with type " +
        "'{FunctionBlockDesignNamespace}.{FunctionBlockDesignName}'.")]
    public static partial void FunctionBlockInitializationErrorOccured(this ILogger logger, Exception ex, byte initPhaseNo,
        Guid functionBlockId, string functionBlockName, string functionBlockDesignNamespace, string functionBlockDesignName);
}
